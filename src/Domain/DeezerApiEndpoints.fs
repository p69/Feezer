namespace Feezer.Domain

module DeezerApi =
  let mutable private accessToken = ""
  let baseUri = "https://api.deezer.com/"

  let setToken token = accessToken <- token

  let constructUriWithToken path = sprintf "%s%s?access_token=%s" baseUri path accessToken

  module User =
    let private constrcutUserUri method =  sprintf "user/%s" method |> constructUriWithToken
    let me() = constrcutUserUri "me"
    let flow() = constrcutUserUri "flow"