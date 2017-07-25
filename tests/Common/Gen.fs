namespace Tests.Common

module GenEx =
    open FsCheck
    open System
    open Expecto

    type Float01 = Float01 of float
    let float01Arb =
        let maxValue = float UInt32.MaxValue
        Arb.convert
            (fun (DoNotSize a) -> float a / maxValue |> Float01)
            (fun (Float01 f) -> f * maxValue + 0.5 |> uint64 |> DoNotSize)
            Arb.from
    type 'a ListOf100 = ListOf100 of 'a list
    let listOf100Arb() =
        Gen.listOfLength 100 Arb.generate
        |> Arb.fromGen
        |> Arb.convert ListOf100 (fun (ListOf100 l) -> l)
    type 'a ListOfAtLeast2 = ListOfAtLeast2 of 'a list
    let listOfAtLeast2Arb() =
        Arb.convert
            (fun (h1,h2,t) -> ListOfAtLeast2 (h1::h2::t))
            (function
                | ListOfAtLeast2 (h1::h2::t) -> h1,h2,t
                | e -> failwithf "not possible in listOfAtLeast2Arb: %A" e)
            Arb.from

    type 'a ListOfAtLeast1 = ListOfAtLeast1 of 'a list
    let listOfAtLeast1Arb() =
        Arb.convert
            (fun (h1,t) -> ListOfAtLeast1 (h1::t))
            (function
                | ListOfAtLeast1 (h1::t) -> h1,t
                | e -> failwithf "not possible in listOfAtLeast2Arb: %A" e)
            Arb.from

    type NonEmptyString = NonEmptyString of string
    let nonEmptyStringArb =
        Arb.convert
            (fun str -> if (String.IsNullOrEmpty str) then NonEmptyString("") else NonEmptyString(str))
            (fun (NonEmptyString str) -> str)
            Arb.from

    type ValidASCIIString = ValidASCIIString of string
    let private asciiRegex = Text.RegularExpressions.Regex("""[[:ascii:]]*""",
                                     Text.RegularExpressions.RegexOptions.IgnoreCase|||Text.RegularExpressions.RegexOptions.CultureInvariant|||Text.RegularExpressions.RegexOptions.Compiled)
    let validASCIIStringArb =
        Arb.convert
            (fun str ->
                if (String.IsNullOrEmpty str || (not <| asciiRegex.IsMatch str)) then ValidASCIIString("")
                else  ValidASCIIString(str.Trim()))
            (fun (ValidASCIIString str) -> str)
            Arb.from

    let addToConfig config =
        {config with arbitrary = typeof<Float01>.DeclaringType::config.arbitrary}