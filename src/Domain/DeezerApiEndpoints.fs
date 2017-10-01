namespace Feezer.Domain

module DeezerApi =
  let mutable private accessToken = ""
  let baseUri = "https://api.deezer.com/"

  let setToken token = accessToken <- token

  let constructUriWithToken path = baseUri+path+"?access_token="+accessToken

  module User =
    let private constrcutUserUri method = constructUriWithToken "user/"+method
    let me = constrcutUserUri "me"
    let flow = constrcutUserUri "flow"