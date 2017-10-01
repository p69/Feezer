namespace Feezer.Server.ActorModel.FSharpApi

open System
open System.Threading.Tasks
open Microsoft.FSharp.Quotations
open Proto
open Proto.Mailbox
open Feezer.Utils


[<AutoOpen>]
module ActorApi =
    type MessagesHandler = IContext->unit
    type SendInterceptor = ISenderContext->PID->MessageEnvelope->unit

    let private toReceiveDelegate handler (next:Receive) =
      let receive ctx =
          async {
            handler ctx
            do! next.Invoke(ctx) |> Async.AwaitTask
          } |> Async.AsTask
      Receive(receive)

    let private toFuncReceiveMiddleware middleware =
      new Func<Receive, Receive>(fun next ->
          toReceiveDelegate middleware next
      )

    let private toSendDelegate handler (next:Sender) =
      let send ctx pid envelop =
          async {
              handler ctx pid envelop
              do! next.Invoke(ctx, pid, envelop) |> Async.AwaitTask
          } |> Async.AsTask
      Sender(send)

    let private toFuncSendMiddleware middleware =
      new Func<Sender, Sender>(fun next ->
          toSendDelegate middleware next
      )
    let behaviorToReceive handler =
        let receive ctx =
            async {
              handler ctx
            } |> Async.AsTask
        Receive(receive)

    type BehaviorSwitcher = {
      become:(IContext->unit)->unit
      becomeStacked:(IContext->unit)->unit
      unbecomeStacked:unit->unit
    }
    type MessageHandlerWithBahavior = BehaviorSwitcher->IContext->unit

    type PropsConfig = {
      Mailbox:(unit->IMailbox) option
      Dispatcher:IDispatcher option
      SupervisorStrategy:ISupervisorStrategy option
      ReceiveMiddlewares:list<MessagesHandler>
      SendMiddleware:list<SendInterceptor>
      Behaviors:(BehaviorSwitcher->list<MessagesHandler>) option
    }
    let private zeroConfig = {
      Mailbox=None
      Dispatcher=None
      SupervisorStrategy=None
      ReceiveMiddlewares=[]
      SendMiddleware=[]
      Behaviors=None
    }

    /// The builder for simple actor computation expression.
    type ConfigBuilder() =
      member this.Zero() = zeroConfig
      member this.Yield (()) = this.Zero()

      [<CustomOperation ("mailbox", MaintainsVariableSpace = true)>]
      member this.Mailbox(config, mailbox) =
          {config with Mailbox=Some mailbox}

      [<CustomOperation ("dispatcher", MaintainsVariableSpace = true)>]
      member this.Dispatcher(config, dispatcher) =
          {config with Dispatcher=Some dispatcher}

      [<CustomOperation ("supervisorStrategy", MaintainsVariableSpace = true)>]
      member this.SupervisorStrategy(config, strategy) =
          {config with SupervisorStrategy=Some strategy}

      [<CustomOperation ("receiveMiddlewares", MaintainsVariableSpace = true)>]
      member this.ReceiveMiddlewares(config, middlewares) =
          {config with ReceiveMiddlewares=middlewares}

      [<CustomOperation ("sendMiddlewares", MaintainsVariableSpace = true)>]
      member this.SendMiddlewares(config, middlewares) =
          {config with SendMiddleware=middlewares}

      [<CustomOperation ("behaviors", MaintainsVariableSpace = true)>]
      member this.Behaviors(config, behaviors) =
          {config with Behaviors=Some behaviors}

      member this.Run(config:PropsConfig) = config


    ///Simple actor
    let config = ConfigBuilder()

    type IO<'T> =
        | Input
    /// Gives access to the next message throu let! binding in actor computation expression.
    type Cont<'In, 'Out> =
        | Func of ('In -> Cont<'In, 'Out>)
        | Return of 'Out

    [<Interface>]
    type Actor<'Context, 'Message when 'Context :> IContext> =
      abstract Receive: unit->IO<'Context*'Message>

      /// The builder for actor computation expression.
    type ActorBuilder() =

        /// Binds the next message.
        member __.Bind(m : IO<'In>, f : 'In -> _) = Func(fun m -> f m)

        /// Binds the result of another actor computation expression.
        member this.Bind(x : Cont<'In, 'Out1>, f : 'Out1 -> Cont<'In, 'Out2>) : Cont<'In, 'Out2> =
            match x with
            | Func fx -> Func(fun m -> this.Bind(fx m, f))
            | Return v -> f v

        member __.ReturnFrom(x) = x
        member __.Return x = Return x
        member __.Zero() = Return()

        member this.TryWith(f : unit -> Cont<'In, 'Out>, c : exn -> Cont<'In, 'Out>) : Cont<'In, 'Out> =
            try
                true, f()
            with ex -> false, c ex
            |> function
            | true, Func fn -> Func(fun m -> this.TryWith((fun () -> fn m), c))
            | _, v -> v

        member this.TryFinally(f : unit -> Cont<'In, 'Out>, fnl : unit -> unit) : Cont<'In, 'Out> =
            try
                match f() with
                | Func fn -> Func(fun m -> this.TryFinally((fun () -> fn m), fnl))
                | r ->
                    fnl()
                    r
            with ex ->
                fnl()
                reraise()

        member this.Using(d : #IDisposable, f : _ -> Cont<'In, 'Out>) : Cont<'In, 'Out> =
            this.TryFinally((fun () -> f d),
                            fun () ->
                                if d <> null then d.Dispose())

        member this.While(condition : unit -> bool, f : unit -> Cont<'In, unit>) : Cont<'In, unit> =
            if condition() then
                match f() with
                | Func fn ->
                    Func(fun m ->
                        fn m |> ignore
                        this.While(condition, f))
                | v -> this.While(condition, f)
            else Return()

        member __.For(source : 'Iter seq, f : 'Iter -> Cont<'In, unit>) : Cont<'In, unit> =
            use e = source.GetEnumerator()

            let rec loop() =
                if e.MoveNext() then
                    match f e.Current with
                    | Func fn ->
                        Func(fun m ->
                            fn m |> ignore
                            loop())
                    | r -> loop()
                else Return()
            loop()

        member __.Delay(f : unit -> Cont<_, _>) = f
        member __.Run(f : unit -> Cont<_, _>) = f()
        member __.Run(f : Cont<_, _>) = f

        member this.Combine(f : unit -> Cont<'In, _>, g : unit -> Cont<'In, 'Out>) : Cont<'In, 'Out> =
            match f() with
            | Func fx -> Func(fun m -> this.Combine((fun () -> fx m), g))
            | Return _ -> g()

        member this.Combine(f : Cont<'In, _>, g : unit -> Cont<'In, 'Out>) : Cont<'In, 'Out> =
            match f with
            | Func fx -> Func(fun m -> this.Combine(fx m, g))
            | Return _ -> g()

        member this.Combine(f : unit -> Cont<'In, _>, g : Cont<'In, 'Out>) : Cont<'In, 'Out> =
            match f() with
            | Func fx -> Func(fun m -> this.Combine((fun () -> fx m), g))
            | Return _ -> g

        member this.Combine(f : Cont<'In, _>, g : Cont<'In, 'Out>) : Cont<'In, 'Out> =
            match f with
            | Func fx -> Func(fun m -> this.Combine(fx m, g))
            | Return _ -> g


    type FunActor<'Message,'Returned>(actor : Actor<IContext,'Message> -> Cont<IContext*'Message, 'Returned>, config:PropsConfig) =
        let mutable state =
            actor { new Actor<IContext, 'Message> with
                        member __.Receive() = Input}

        let behavior = maybe {
            let! behaviorsList = config.Behaviors
            return Behavior()
          }

        let bahoviorSwitcher = maybe {
            let! nativeBehavior = behavior
            let switcher = {
              become=
                fun f ->
                  let handler = behaviorToReceive f
                  nativeBehavior.Become(handler)
              becomeStacked=
                fun f ->
                  let handler = behaviorToReceive f
                  nativeBehavior.BecomeStacked(handler)
              unbecomeStacked=fun () -> nativeBehavior.UnbecomeStacked()
            }
            let! configFunction = config.Behaviors
            let behaviorsList = configFunction switcher
            behaviorToReceive <| behaviorsList.[0] |> nativeBehavior.Become
            return switcher
          }

        interface IActor with
          member x.ReceiveAsync(ctx) =
            async {
              maybe {
                let! behaviorHandler = behavior
                async {
                  do! behaviorHandler.ReceiveAsync(ctx)|>Async.AwaitTask
                } |> Async.RunSynchronously
              } |> ignore

              match state with
              | Func f ->
                match ctx.Message with
                | :?'Message as msg -> state <- f (ctx, msg)
                | _ ->()
              | Return _ -> ctx.Self.Stop()

            } |> Async.AsTask

    /// Builds an actor message handler using an actor expression syntax.
    let actor = ActorBuilder()

    let propsWithConfig (cfg:PropsConfig) (body:Actor<IContext,'Message> -> Cont<IContext*'Message, 'Returned>) =
        let mutable props =
                Actor.FromProducer(
                  fun () -> FunActor(body, cfg) :> IActor
                )
        maybe {
          let! mailBox = cfg.Mailbox
          props <- props.WithMailbox(
              fun () -> mailBox()
          )
        } |> ignore
        maybe {
          let! dispatcher = cfg.Dispatcher
          props <- props.WithDispatcher dispatcher
        } |> ignore
        maybe {
          let! strategy = cfg.SupervisorStrategy
          props <- props.WithChildSupervisorStrategy strategy
        } |> ignore
        props<-
          props
            .WithReceiveMiddleware(cfg.ReceiveMiddlewares|>List.map toFuncReceiveMiddleware|>List.toArray)
            .WithSenderMiddleware(cfg.SendMiddleware|>List.map toFuncSendMiddleware|>List.toArray)
        props

    let props (body:Actor<IContext,'Message> -> Cont<IContext*'Message, 'Returned>) = propsWithConfig zeroConfig body

    let withMailbox (m:unit->IMailbox) (props:Props) = props.WithMailbox (fun ()->m())
    let withSupervisorStrategy s (props:Props) = props.WithChildSupervisorStrategy s
    let withReceiveMiddleware m (props:Props) = props.WithReceiveMiddleware(m|>List.map toFuncReceiveMiddleware|>List.toArray)
    let withSenderMiddleware m (props:Props) = props.WithSenderMiddleware(m|>List.map toFuncSendMiddleware|>List.toArray)
    let withDispatcher d (props:Props) = props.WithDispatcher d
    let withSpawner s (props:Props) = props.WithSpawner s

    let spawnProps props = props |> Actor.Spawn
    let spawn body = props body |> spawnProps
    let spawnPropsNamed name props = Actor.SpawnNamed(props, name)
    let spawnPropsPrefix prefix props = Actor.SpawnPrefix(props, prefix)
    let spawnPropsFromContext (ctx:IContext) props = ctx.Spawn(props)
    let spawnPropsNamedFromContext name (ctx:IContext) props = ctx.SpawnNamed(props, name)
    let spawnPropsPrefixFromContext prefix (ctx:IContext) props = ctx.SpawnPrefix(props, prefix)
    let spawnNamed name body = body |> props |> spawnPropsNamed name
    let spawnPrefix prefix body = body |> props |> spawnPropsPrefix prefix
    let spawnFromContext ctx body = body |> props |> spawnPropsFromContext ctx
    let spawnNamedFromContext name ctx body = body |> props |> spawnPropsNamedFromContext name ctx
    let spawnPrefixFromContext prefix ctx body = body |> props |> spawnPropsPrefixFromContext prefix ctx
    let spawnWithConfig (cfg:PropsConfig) body = propsWithConfig cfg body |> spawnProps

    let actorOf (fn : IContext*'Message -> unit) (mailbox : Actor<IContext,'Message>) =
      let rec loop() =
          actor {
              let! msg = mailbox.Receive()
              fn msg
              return! loop()
          }
      loop()


    let tell<'a> (msg:'a) (pid:PID) = pid.Tell(msg)
    let inline (<!) p m = tell m p
    let request<'a> (msg:'a) (receiver:PID) (sender:PID) = receiver.Request(msg, sender)
    let inline (<?) p (m,s) = request m p s
    let requestAsync<'a, 'b> (msg:'a) (receiver:PID) : Async<'b>=
      async {
          let! result = receiver.RequestAsync(msg) |> Async.AwaitTask
          return result
      }
    let inline (<??) p m = requestAsync m p

    let matchMessage<'a> f message =
      match box message with
      | :? 'a as msg -> f msg
      | _ -> ()

    let inline (>>|) m f = matchMessage f m
    let pipeToMessage<'a> f message =
      match box message with
      | :? 'a as msg ->
          f msg
          message
      | _ -> message

    let inline (>>=) m f = pipeToMessage f m