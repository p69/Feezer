module Feezer.Client.Main

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Node.Exports


// Convinience operator to ignore results
let inline (!>) x = ignore x

// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mutable mainWindow: BrowserWindow option = Option.None

let createMainWindow () =
    let options = createEmpty<BrowserWindowOptions>
    options.width <- Some 800.
    options.height <- Some 600.

    //we need this to make grid work
    let webPreferences = createEmpty<WebPreferences>
    webPreferences.experimentalFeatures <- Some true
    options.webPreferences <- Some webPreferences

    let window = electron.BrowserWindow.Create(options)

    // Load the index.html of the app.
    window.loadURL("file://" + Node.Globals.__dirname + "/../index.html");

    #if DEBUG
    Fs.watch(Path.join(Node.Globals.__dirname, "/renderer.js"), fun _ _ ->
        window.webContents.reloadIgnoringCache()
    ) |> ignore
    #endif

    // Emitted when the window is closed.
    !>window.on("closed", !!(fun () ->
        // Dereference the window object, usually you would store windows
        // in an array if your app supports multi windows, this is the time
        // when you should delete the corresponding element.
        mainWindow <- Option.None
    ))

    mainWindow <- Some window

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
!>electron.app.on("ready", !!createMainWindow)

// Quit when all windows are closed.
!>electron.app.on("window-all-closed", !!(fun () ->
    // On OS X it is common for applications and their menu bar
    // to stay active until the user quits explicitly with Cmd + Q
    if Node.Globals.``process``.platform <> Node.Base.NodeJS.Darwin then
        electron.app.quit()
))

!>electron.app.on("activate", !!(fun () ->
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if mainWindow.IsNone then
        createMainWindow()
))
