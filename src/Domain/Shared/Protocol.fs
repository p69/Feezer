namespace Feezer.Domain

module Protocol =
    open System

    type UserInfo = {
        name:string;
        avatar:Uri
    }

    type TokenExpiration =
        | Never
        | Date of DateTime

    type Server =
        | Authorization of url:string
        | Authorized of expiration:TokenExpiration
        | UserInfo of UserInfo

    type Client =
        | Authozrize