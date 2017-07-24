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
