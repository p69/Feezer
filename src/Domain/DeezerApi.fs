namespace Feezer.Domain

module DeezerApi =
  let mutable private accessToken = ""

  let baseUri = "https://api.deezer.com/"

  let setToken token = accessToken <- token

  let constructUriWithToken path = sprintf "%s%s?access_token=%s" baseUri path accessToken

  module User =

    type AuthorizationResultJson = {
      access_token:string;
      expires:int
    }

    type UserJson = {
      id:int;
      name:string;
      picture_small:string;
      picture_medium:string;
      picture_big:string;
      country:string;
      lang:string;
      tracklist:string;
    }

    type FlowTrackJson = {
      id:int;
      readable:bool;
      title:string;
      title_short:string;
      title_version:string;
      duration:int; //in seconds
      rank:int; //Deezer rank
      explicit_lyrics:bool;
      preview:string;//track preview url (first 30 seconds)
    }

    type FlowJson = FlowTrackJson list

    let private constrcutUserUri method =  sprintf "user/%s" method |> constructUriWithToken
    let me() = constrcutUserUri "me"
    let flow() = constrcutUserUri "flow"
