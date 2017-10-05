namespace Feezer.Domain

module Protocol =
    open System
    open Feezer.Domain.User


    type TokenExpiration =
        | Never
        | Date of DateTime

    type Server =
        | Authorization of url:string
        | CurrentUser of User

    type Client =
        | GetUser
        | Authorize