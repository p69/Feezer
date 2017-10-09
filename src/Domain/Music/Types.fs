module Feezer.Domain.Music.Types

open System

type FlowTrack = {
    deezerId:int;
    fullTitle:string;
    shortTitle:string;
    duration:TimeSpan;
    deezerRank:int;
    hasLyrics:bool;
    previewUrl:string;
    url:string
}

type Flow = {tracks:FlowTrack list}