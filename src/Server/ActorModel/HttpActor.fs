namespace Feezer.Server.ActorModel.Http
open Feezer.Server.ActorModel.FSharpApi
open Proto
open System.Net.Http

type Request =
    | GET of uri:string
    | POST of uri:string*body:string

type Response =
    | Success of uri:string*result:string
    | Error of uri:string*error:string

module HttpActor =
  let private doRequest (sender:PID option) request =
    async {
        let client = new HttpClient()
        let (task, uri) =
            match request with
            | GET uri -> (client.GetAsync(uri), uri)
            | POST (uri, content) ->
                let postBody = new FormUrlEncodedContent(dict["key", content])
                (client.PostAsync(uri, postBody), uri)
        try
            let! response = task |> Async.AwaitTask
            let! result = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            Option.iter (fun s -> s <! Ok(uri, result)) sender
        with
        | exn as ex -> Option.iter (fun s -> s <! Error(uri, ex.ToString())) sender
        return ()
    }
  let create () =
    actor {
      receive (fun ctx ->
        ctx.Message
        >>| fun (msg:Request) ->
              let sender = if isNull ctx.Sender then None else Some ctx.Sender
              msg |> doRequest sender |> Async.RunSynchronously
      )
    }