namespace Feezer.Domain.Protocol

type Server =
    | Authorization of url:string
    | Authorized of token:string