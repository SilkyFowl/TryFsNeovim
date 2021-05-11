module Tests

open Expecto
open Expecto.Flip
open FsCheck
open FsNeovim.MessagePackRpc
open System.IO

let convert (stream: Stream) token value =
    async {
        let writer = getWriter stream
        let reader = getReader stream

        Request value |> serializeMessage writer token
        do! writer.FlushAsync().AsAsync() |> Async.Ignore

        stream.Seek(0L, SeekOrigin.Begin) |> ignore

        let! readResult = reader.ReadAsync(token).AsAsync()
        let seq = readResult.Value
        let dto = deserializeMessage &seq token

        match dto with
        | Request x -> return x
        | _ -> return failwith "Convert Error"
    }

type RequestGen<'T> =
    static member Request() : Arbitrary<_> =
        let genMsgid = Arb.generate<uint>
        let genMethod = Arb.generate<string>
        let genParameters = Arb.generate<'T> |> Gen.listOf

        let create msgid method parameters =
            { msgid = msgid
              method = method
              parameters = parameters }

        let genReqest =
            create <!> genMsgid
            <*> genMethod
            <*> genParameters

        genReqest |> Arb.fromGen

let reqConfig<'T> =
    { FsCheckConfig.defaultConfig with
          maxTest = 10000
          arbitrary = [ typeof<RequestGen<'T>> ] }

let testReqProp<'T when 'T: equality> name =
    testPropertyWithConfig reqConfig<'T> name
    <| fun (req: Request<'T>) ->
        async {
            let stream = new MemoryStream()
            let token = Async.DefaultCancellationToken

            let! actual = req |> convert stream token

            actual.msgid
            |> Expect.equal "id should be equal" req.msgid

            actual.method
            |> Expect.equal "method should be equal" req.method

            actual.parameters
            |> Expect.sequenceEqual "params should be equal" req.parameters
        }
        |> Async.RunSynchronously

let testReqPropError<'T when 'T: equality> name errorTest =
    testPropertyWithConfig reqConfig<'T> name
    <| fun (req: Request<'T>) ->
        async {
            let stream = new MemoryStream()
            let token = Async.DefaultCancellationToken

            let! actual = req |> convert stream token

            actual.msgid
            |> Expect.equal "id should be equal" req.msgid

            actual.method
            |> Expect.equal "method should be equal" req.method

            try
                actual.parameters
                |> Expect.sequenceEqual "params should be equal" req.parameters
            with :? AssertException as err -> Seq.iter2 errorTest req.parameters actual.parameters
        }
        |> Async.RunSynchronously


[<Tests>]
let tests =
    testList "message"
    <| [ testReqProp<int> "Request<int> Test"
         testReqProp<string> "Request<string> Test"
         testReqProp<bool> "Request<bool> Test"
         testReqProp<byte> "Request<byte> Test"

         testReqPropError<_> "Issue: Request<_> deserialize not work"
         <| fun expect actual ->
             if expect <> null
                && expect.GetType() <> actual.GetType() then
                 actual.GetType()
                 |> Expect.equal "may cause parameters to be of type byte." typeof<byte>

         testReqPropError<obj> "Issue: Request<obj> deserialize not work"
         <| fun expect actual ->
             if expect <> null
                && expect.GetType() <> actual.GetType() then
                 actual.GetType()
                 |> Expect.equal "may cause parameters to be of type byte." typeof<byte>

         ]
