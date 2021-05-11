module FsNeovim.MessagePackRpc

open MessagePack
open MessagePack.FSharp
open MessagePack.Resolvers
open System.Buffers
open System.IO
open System.IO.Pipelines

open type MessagePack.MessagePackSerializer
open Microsoft.FSharp.Core.LanguagePrimitives

type TypeId =
    | Request = 0uy
    | Response = 1uy
    | Notification = 2uy

type Request<'TParam> =
    { msgid: uint
      method: string
      parameters: 'TParam seq }

type Response<'TResult, 'TError> =
    { msgid: uint
      error: 'TError
      result: 'TResult }

type Notification<'TParam> =
    { method: string
      parameters: 'TParam seq }

[<MessagePackObject>]
type RequestMessage<'TParam> =
    { [<Key(0)>]
      typeid: byte
      [<Key(1)>]
      msgid: uint
      [<Key(2)>]
      method: string
      [<Key(3)>]
      parameters: 'TParam seq }

[<MessagePackObject>]
type ResponseMessage<'TResult, 'TError> =
    { [<Key(0)>]
      typeid: byte
      [<Key(1)>]
      msgid: uint
      [<Key(2)>]
      error: 'TError
      [<Key(3)>]
      result: 'TResult }

[<MessagePackObject>]
type NotificationMessage<'TParam> =
    { [<Key(0)>]
      typeid: byte
      [<Key(1)>]
      method: string
      [<Key(2)>]
      parameters: 'TParam seq }

type MsgPackRpcDto<'TParam, 'TResult, 'TError> =
    | Request of Request<'TParam>
    | Response of Response<'TResult, 'TError>
    | Notification of Notification<'TParam>

    static member ofMessagePackObject(reqest: RequestMessage<'TParam>) =
        Request
            { msgid = reqest.msgid
              method = reqest.method
              parameters = reqest.parameters }

    static member ofMessagePackObject(responce: ResponseMessage<'TResult, 'TError>) =
        Response
            {  msgid = responce.msgid
               error = responce.error
               result = responce.result }

    static member ofMessagePackObject(notification: NotificationMessage<'TParam>) =
        Notification
            {  method = notification.method
               parameters = notification.parameters }


/// Wrapper function. Serialize into IBufferWriter.
let serialize<'T> (writer: IBufferWriter<byte>) options cancellationToken (value: 'T) =
    Serialize<'T>(writer, value, options, cancellationToken)


let deserialize<'T> (byteSequence: inref<ReadOnlySequence<byte>>) options cancellationToken =
    Deserialize<'T>(&byteSequence, options, cancellationToken)

let private options =
    Resolvers.CompositeResolver.Create(FSharpResolver.Instance, StandardResolver.Instance)
    |> MessagePackSerializerOptions.Standard.WithResolver

/// Serialize into IBufferWriter.
let serializeMessage writer cancellationToken value =
    match value with
    | Request req ->
        { typeid = byte TypeId.Request
          msgid = req.msgid
          method = req.method
          parameters = req.parameters }
        |> serialize writer options cancellationToken

    | Response res ->
        { typeid = byte TypeId.Response
          msgid = res.msgid
          error = res.error
          result = res.result }
        |> serialize writer options cancellationToken

    | Notification notify ->
        { typeid = byte TypeId.Notification
          method = notify.method
          parameters = notify.parameters }
        |> serialize writer options cancellationToken


let deserializeMessage<'TParam, 'TResult, 'TError> (byteSequence: inref<ReadOnlySequence<byte>>) cancellationToken =
    let typeid : TypeId =
        EnumOfValue(byteSequence.Slice(1, 1).ToArray().[0])

    match typeid with
    | TypeId.Request ->
        deserialize<RequestMessage<'TParam>> &byteSequence options cancellationToken
        |> MsgPackRpcDto<'TParam, 'TResult, 'TError>
            .ofMessagePackObject

    | TypeId.Response ->
        deserialize<ResponseMessage<'TResult, 'TError>> &byteSequence options cancellationToken
        |> MsgPackRpcDto<'TParam, 'TResult, 'TError>
            .ofMessagePackObject

    | TypeId.Notification ->
        deserialize<NotificationMessage<'TParam>> &byteSequence options cancellationToken
        |> MsgPackRpcDto<'TParam, 'TResult, 'TError>
            .ofMessagePackObject

    | _ -> invalidArg "byteSequence" "typeid is wrong."

let getWriter (stream: Stream) =
    PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen = true))

let getReader stream = new MessagePackStreamReader(stream)
