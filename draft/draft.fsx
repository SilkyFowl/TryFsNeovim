open MessagePack.FSharp
open MessagePack.Resolvers

#r "/workspaces/FsNeovim/src/FsNeovim/bin/Debug/net5.0/FsNeovim.dll"
#r "nuget: MessagePack"
#r "nuget: MessagePack.FSharpExtensions"
#r "nuget: Expecto.FsCheck"
#r "nuget: System.IO.Pipelines"
#r "nuget: FSharp.Control.FusionTasks"
#r "nuget: FSharp.Data"

open FSharp.Data

open Expecto
open FsNeovim.MessagePackRpc
open System.IO
open FsCheck
open MessagePack
open MessagePack
open Expecto.Logging
open System
type User =
    { Id: int
      FirstName: string
      LastName: string }

type UserGen() =
    static member User() : Arbitrary<User> =
        let genFirsName = Gen.elements [ "Don"; "Henrik"; null ]
        let genLastName = Gen.elements [ "Syme"; "Feldt"; null ]

        let createUser id firstName lastName =
            { Id = id
              FirstName = firstName
              LastName = lastName }

        let getId = Gen.choose (0, 1000)

        let genUser =
            createUser <!> getId
            <*> genFirsName
            <*> genLastName

        genUser |> Arb.fromGen

type RequestGen =
    static member MsgPackRpcDto() : Arbitrary<_> =
        let genMsgid = Arb.generate<uint>
        let genMethod = Arb.generate<string>
        let genParameters = Arb.generate<obj> |> Gen.listOf

        let create msgid method parameters =
            {| msgid = msgid
               method = method
               parameters = parameters |}

        let genReqest =
            create <!> genMsgid
            <*> genMethod
            <*> genParameters

        genReqest |> Arb.fromGen

RequestGen.MsgPackRpcDto()

let config =
    { FsCheckConfig.defaultConfig with
          maxTest = 10000 }


let convert (stream: Stream) token value =
    async {
        let writer = getWriter stream
        let reader = getReader stream

        value |> serializeMessage writer token
        do! writer.FlushAsync().AsAsync() |> Async.Ignore

        stream.Seek(0L, SeekOrigin.Begin) |> ignore

        let! readResult = reader.ReadAsync(token).AsAsync()
        let seq = readResult.Value
        let result = deserializeMessage &seq token
        return result
    }


let prop_simple2 () =
    let s =
        Arb.generate<string>
        |> Gen.sample 10 1
        |> List.head
    // let! x = save_to_db s // for example
    printfn "simple2: s = %A" s
    0 < 1

let prop_async3 =
    async {
        let r =
            gen {
                let! s = Arb.generate<string>
                printfn "async3: s = %A" s
                return 0 < 1
            }

        return r
    }
    |> Async.RunSynchronously

prop_async3
Gen.choose (0, 9) |> Gen.sample 0 10


let genReqest () =
    let genMsgid = Arb.generate<uint>
    let genMethod = Arb.generate<string>
    let genParameters = Arb.generate<obj> |> Gen.listOf

    let create msgid method parameters =
        {| msgid = msgid
           method = method
           parameters = parameters |}

    create <!> genMsgid
    <*> genMethod
    <*> genParameters

genReqest () |> Gen.sample 10 1 |> List.head

let ff (x: {| msgid: byte |}) = x.msgid


[<MessagePackObject>]
type Request<'TParam> =
    { [<Key(1)>]
      msgid: uint
      [<Key(2)>]
      method: string
      [<Key(3)>]
      parameters: 'TParam seq }

[<MessagePackObject>]
type RequestMessage<'TParam> =
    { [<Key(0)>]
      typeid: byte
      message: Request<'TParam> }

let r =
    { typeid = 0uy
      message =
          { msgid = 1u
            method = ""
            parameters = seq { 0 } } }

let re =
    { msgid = 1u
      method = ""
      parameters = [ 0 ] }

let options =
    Resolvers.CompositeResolver.Create(FSharpResolver.Instance, StandardResolver.Instance)
    |> MessagePackSerializerOptions.Standard.WithResolver

MessagePackSerializer.Serialize(re)
let stream = new MemoryStream()
let writer = getWriter stream
let reader = getReader stream
let token = Async.DefaultCancellationToken

10 |> serialize writer options token
MessagePackSerializer.Serialize(10)


let x = seq { 1 .. 10 }

match Array.ofSeq x with
| [| x |] -> Some x
| _ -> None



[<Literal>]
let autocomplete =
    "https://azuresearch-usnc.nuget.org/autocomplete"

let queryId q =
    autocomplete + $"?q={q}&prerelease=true"

let queryVersion id =
    autocomplete + $"?id={id}&prerelease=true"

type NugetAutoComplete = JsonProvider<autocomplete>
queryId "MessagePack" |> NugetAutoComplete.Load

NugetAutoComplete.Load(queryId "MessagePack").Data
NugetAutoComplete.Load(queryVersion "MessagePack").Data
|> Array.sortByDescending(fun d -> d.Replace("-\w+$",""))
|> Array.take 10
async {
    let! res = NugetAutoComplete.AsyncLoad(queryVersion "MessagePack")
    return res.Data
}

type NugetApi = JsonProvider<"https://api.nuget.org/v3/index.json">
NugetApi.GetSample()
