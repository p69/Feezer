namespace Feezer.Utils

[<AutoOpen>]
module Maybe =
    type OptionBuilder() =
      member x.Bind(v, f) = Option.bind f v
      member x.Return(v) = Some v
      member x.Zero() = None
      member x.Combine(v, f:unit -> _) = Option.bind f v
      member x.Delay(f : unit -> 'T) = f
      member x.Run(f) = f()
      member x.While(cond, f) =
        if cond() then x.Bind(f(), fun _ -> x.While(cond, f))
        else x.Zero()
    let maybe = OptionBuilder()