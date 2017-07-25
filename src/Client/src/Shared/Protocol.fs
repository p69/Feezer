namespace Feezer.Domain

module Protocol =
    open System

    type TokenExpiration = 
        | Never
        | Date of DateTime

    type Server =
        | Authorization of url:string
        | Authorized of expiration:TokenExpiration

    type Client =
        | Authozrize