open NuGet.Protocol
open NuGet.Protocol.Core.Types
open NuGet.Common

#r "nuget: NuGet.Protocol, 5.9.1"
#r "nuget: FSharp.Control.AsyncSeq"
#r "nuget: FSharp.Control.FusionTasks"

let logger = NullLogger.Instance

let repository =
    Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json")

let resource =
    repository.GetResource<PackageSearchResource>()

let searchFilter =
    new SearchFilter(includePrerelease = true)

let token = Async.DefaultCancellationToken

let results =
    resource.SearchAsync("MessagePack.FSharpExtensions", searchFilter, 0, 20, logger, token)
    |> Async.AwaitTask

let searchAsync searchTerm = async{
    let! results = resource.SearchAsync(searchTerm, searchFilter, 0, 20, logger, token)

    results
    |> Seq.iter (fun r ->
        printfn "%A" r.Identity
        printfn "%A" r.Summary
    )
}

let search searchTerm = searchAsync searchTerm |> Async.RunSynchronously
logger.ToJson()
search "MessagePack Rpc"