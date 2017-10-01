namespace Feezer.Domain.User

open System
open Chiron

module DI = Chiron.Inference.Json.Decode

type User = {
    id: int;
    name: string;
    avatar: Uri
  } with
    static member FromJson(_:User) = jsonDecoder {
      let! id = DI.required "id"
      let! name = DI.required "name"
      let! avatarUrl = DI.required "picture_small"
      return {id=id; name=name; avatar=Uri(avatarUrl)}
    }
