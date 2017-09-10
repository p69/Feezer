namespace Feezer.Server
open Newtonsoft.Json

module Utils =
  let private jsonConverter = Fable.JsonConverter() :> JsonConverter
  let fromJson<'a> value = JsonConvert.DeserializeObject<'a>(value, [|jsonConverter|])
  let toJson value = JsonConvert.SerializeObject(value, [|jsonConverter|])
