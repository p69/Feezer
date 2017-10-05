namespace Feezer.Domain.User

open System

type UserInfo = {
    id: int;
    name: string;
    avatar: string
  }

type User =
| Anonymous
| Authorized of info:UserInfo

