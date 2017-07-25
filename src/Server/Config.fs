namespace Feezer.Server

module Config =
    open System.IO
    open System
    open Newtonsoft.Json
    open Feezer.Utils

    type AppConfig = {
        DeezerAppSecret:string;
        DeezerAppId:string
    }

    let private salt = "JIAFE7CNHGEM2CVI"

    let mutable private passPhrase:string option = None

    let private readPassword () =
        let readFromFile fileName =
            if (not <| File.Exists fileName) then failwith "Couldn't find file with passphrase, make sure that file 'pass.phr' exists in the src/Server folder"
            File.ReadAllText fileName

        match passPhrase with
        | Some phrase -> phrase
        | None ->
            passPhrase <- Some (readFromFile <| Path.Combine(Directory.GetCurrentDirectory(), "src", "Server", "pass.phr"))
            passPhrase.Value

    let private readConfigJson fileName =
        if (not <| File.Exists fileName) then failwith "Couldn't find 'appconfig.json', make sure that file exists in src/Server folder"
        JsonConvert.DeserializeObject<AppConfig> <| File.ReadAllText fileName

    let get () =
        let configPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Server", "appconfig.json")
        let config = readConfigJson configPath
        let password = readPassword ()
        let decodedAppSecrete = Cryptography.decrypt config.DeezerAppSecret password salt
        {config with DeezerAppSecret=decodedAppSecrete}
