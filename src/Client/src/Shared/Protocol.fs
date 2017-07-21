namespace Feezer.Domain

module Protocol =
    type Server =
        | Authorization of url:string
        | Authorized of token:string

    type Client =
        | Authozrize