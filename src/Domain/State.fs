namespace Feezer.Domain

module AppState =
    type State =
        | Guest
        | Authorization of url:string
        | Authorized of nick:string

    type Message =
        | NeedAuthorize