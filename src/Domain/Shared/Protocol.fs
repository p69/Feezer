namespace Feezer.Domain

module Protocol =
    open System
    open Feezer.Domain.User


    type TokenExpiration =
        | Never
        | Date of DateTime

    type Server =
        | Authorization of url:string
        | Authorized of expiration:TokenExpiration
        | UserInfo of User

    type Client =
        | Authozrize