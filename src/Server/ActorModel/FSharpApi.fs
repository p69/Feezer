namespace Feezer.Server

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
    type PropsConfig = {
        OnReceive:MessagesHandler option;
        Mailbox:IMailbox option;
        Dispatcher:IDispatcher option;
        SupervisorStrategy:ISupervisorStrategy option;
        ReceiveMiddlewares:array<MessagesHandler>;
        SendMiddleware:array<SendInterceptor>;
        Spawner:Spawner option;
    }

    let private inv (next:MessagesHandler) =
        let current (ctx:IContext) =
            next ctx
        current

    let private toReceiveDelegate handler (next:Receive) =
        let receive ctx =
            async {
                handler ctx
                do! next.Invoke(ctx)
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
                do! next.Invoke(ctx, pid, envelop)
            } |> Async.AsTask
        Sender(send)

    let private toFuncSendMiddleware middleware =
        new Func<Sender, Sender>(fun next ->
            toSendDelegate middleware next
        )

    let private zeroConfig = {
        OnReceive=None;
        Mailbox=None;
        Dispatcher=None;
        SupervisorStrategy=None;
        ReceiveMiddlewares=[||];
        SendMiddleware=[||];
        Spawner=None
    }
    /// The builder for actor computation expression.
    type ActorBuilder() =
        member this.Zero() = zeroConfig
        member this.Yield (()) = this.Zero()

        [<CustomOperation ("receive", MaintainsVariableSpace = true)>]
        member this.Receive(config, handler) =
            {config with OnReceive=Some handler}

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

        member this.Run(cfg:PropsConfig) =
            let result = maybe {
                let! handler = cfg.OnReceive
                let mutable props =
                    Actor.FromFunc(
                      fun ctx ->
                          handler ctx
                          Actor.Done
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
                      .WithReceiveMiddleware(cfg.ReceiveMiddlewares|>Array.map toFuncReceiveMiddleware)
                      .WithSenderMiddleware(cfg.SendMiddleware|>Array.map toFuncSendMiddleware)
                return props
            }
            match result with
            | None -> failwith "You should specify handler at least"
            | Some p -> p

    let actor = ActorBuilder()