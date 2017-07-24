namespace Feezer.Server

module Config =
    open System.IO
    open System
    open Newtonsoft.Json

    type AppConfig = {
        DeezerAppSecret:string
    }

    let readConfigJson fileName =
        if (not <| File.Exists fileName) then failwith "Couldn't find 'appconfig.json', make sure that file exists in src/Server folder"
        JsonConvert.DeserializeObject<AppConfig> <| File.ReadAllText fileName

    let mutable private passPhrase:string option = None

    let private readPassPhrase ()=
        let readFromFile fileName =
            if (not <| File.Exists fileName) then failwith "Couldn't find file with passphrase, make sure that file 'pass.phr' exists in the src/Server folder"
            File.ReadAllText fileName

        match passPhrase with
        | Some phrase -> phrase
        | None ->
            passPhrase <- Some (readFromFile <| Path.Combine(Directory.GetCurrentDirectory(), "..\\Server", "pass.phr"))
            passPhrase.Value
