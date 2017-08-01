namespace ActorApi

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
      OnReceive:MessagesHandler
      OnReceiveBehavior:MessageHandlerWithBahavior option
      Mailbox:IMailbox option
      Dispatcher:IDispatcher option
      SupervisorStrategy:ISupervisorStrategy option
      ReceiveMiddlewares:list<MessagesHandler>
      SendMiddleware:list<SendInterceptor>
      Spawner:Spawner option
      Behaviors:(BehaviorSwitcher->list<MessagesHandler>) option
    }
    let private zeroConfig = {
      OnReceive=ignore
      OnReceiveBehavior=None
      Mailbox=None
      Dispatcher=None
      SupervisorStrategy=None
      ReceiveMiddlewares=[]
      SendMiddleware=[]
      Spawner=None
      Behaviors=None
    }


    let createActor cfg =
      let behavior =
        maybe {
          let! behaviorsList = cfg.Behaviors
          return Behavior()
        }
      let bahoviorSwitcher =
        maybe {
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
          let! configFunction = cfg.Behaviors
          let behaviorsList = configFunction switcher
          behaviorToReceive <| behaviorsList.[0] |> nativeBehavior.Become
          return switcher
        }
      { new IActor
          with member this.ReceiveAsync(ctx) =
                async {
                  maybe {
                    let! behaviorHandler = behavior
                    async {
                      do! behaviorHandler.ReceiveAsync(ctx)|>Async.AwaitTask
                    } |> Async.RunSynchronously
                  } |> ignore
                  match cfg.OnReceiveBehavior with
                  | None -> cfg.OnReceive ctx
                  | Some handler ->
                     maybe {
                      let! b = bahoviorSwitcher
                      handler b ctx
                     }|>ignore
                } |> Async.AsTask
      }

    /// The builder for simple actor computation expression.
    type ActorBuilder() =
      member this.Zero() = zeroConfig
      member this.Yield (()) = this.Zero()

      [<CustomOperation ("receive", MaintainsVariableSpace = true)>]
      member this.Receive(config, handler) =
          {config with OnReceive=handler}

      [<CustomOperation ("receiveWithBehavior", MaintainsVariableSpace = true)>]
      member this.ReceiveWithBehavior(config, handler) =
          {config with OnReceiveBehavior=Some handler}

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

      [<CustomOperation ("spawner", MaintainsVariableSpace = true)>]
      member this.Spawner(config, spawner) =
          {config with Spawner=Some spawner}

      [<CustomOperation ("behaviors", MaintainsVariableSpace = true)>]
      member this.Behaviors(config, behaviors) =
          {config with Behaviors=Some behaviors}

      member this.Run(cfg:PropsConfig) =
        let mutable props =
                Actor.FromProducer(
                  fun () -> createActor cfg
                )
        maybe {
          let! mailBox = cfg.Mailbox
          props <- props.WithMailbox(
              fun () -> mailBox
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
        maybe {
          let! spawner = cfg.Spawner
          props <- props.WithSpawner spawner
        } |> ignore
        props<-
          props
            .WithReceiveMiddleware(cfg.ReceiveMiddlewares|>List.map toFuncReceiveMiddleware|>List.toArray)
            .WithSenderMiddleware(cfg.SendMiddleware|>List.map toFuncSendMiddleware|>List.toArray)
        props

    ///Simple actor
    let actor = ActorBuilder()

    let spawn props = props |> Actor.Spawn
    let spawnNamed name props = Actor.SpawnNamed(props, name)
    let spawnPrefix prefix props = Actor.SpawnPrefix(props, prefix)
    let spawnFromContext (ctx:IContext) props = ctx.Spawn(props)
    let spawnNamedFromContext name (ctx:IContext) props = ctx.SpawnNamed(props, name)
    let spawnPrefixFromContext prefix (ctx:IContext) props = ctx.SpawnPrefix(props, prefix)
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