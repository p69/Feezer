namespace Feezer.Domain

module Protocol =
    open System
    open Feezer.Domain.User
    open Feezer.Domain.Music.Types


    type TokenExpiration =
        | Never
        | Date of DateTime

    type Server =
        | Authorization of url:string
        | CurrentUser of User
        | UsersFlow of Flow

    type Client =
        | GetUser
        | Authorize
        | LoadFlow