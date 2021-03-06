﻿module SpectralPeakTrackTest

open Common
open QutSensors.AudioAnalysis.AED.SpectralPeakTrack
open Xunit

[<Fact>]
let testVerticalPeaks () = 
    let v = loadTestFile2 "SPT" "I3.csv" |> verticalPeaks 9.0
    let f i j = let (s, is) = v.[j] in if Set.contains i is then s.[i] else 0.0
    let i3c = Math.Matrix.init (Array.length (fst v.[0])) v.Length f
    let i3cm = loadTestFile2 "SPT" "I3c.csv" 
    Assert.True (matrixFloatEquals i3cm i3c 0.0001)
    
[<Fact>]
let testIndexMaxNeighbour () =
    let m = Math.Matrix.ofList [[9.0; 10.0; 0.0]; [0.0; 11.0; 3.0]]
    Assert.Equal(None, indexMaxNeighbour 0 ([|10.0; 11.0|], Set.empty))
    Assert.Equal(Some 1, indexMaxNeighbour 0 ([|10.0; 11.0|], Set.ofList [0; 1]))
    
(* TODO Try FsCheck. The following properties should hold
0 <= i < m.NumRows
1 <= nj < m.numCols
Set.isEmpty ni ==> indexMaxNeighbour i ni m nj == None
indexMaxNeighbour i ni m nj == None ==> Set.isEmpty ni || forall x in ni |i-x| > 2
*)  

[<Fact>]
let testHorizontalTracksQuick () =
    Assert.Equal<list<_>>(List.replicate 3 Set.empty, horizontalTracks (List.replicate 3 ([|10.0; 11.0|], Set.empty)))
    Assert.Equal<Set<_>>([Set.ofList [0]; Set.ofList [1]; Set.empty],
        horizontalTracks [([|9.0; 0.0|], Set.ofList [0]); ([|10.0; 11.0|], Set.ofList [0; 1]); ([|10.0; 11.0|], Set.empty)])
    
[<Fact>]
let testHorizontalTracks () =
    let v = loadTestFile2 "SPT" "I3.csv" |> verticalPeaks 9.0
    let peaksI3_h = horizontalTracks v |> colIndicesToMatrix (Array.length (fst v.[0])) v.Length
    let peaksI3_hm = loadTestFile2 "SPT" "peaksI3_h.csv" 
    Assert.True (matrixFloatEquals peaksI3_hm peaksI3_h 0.0001)
    
[<Fact>]
let testAllPeaks () =
    let peaksI3 = loadTestFile2 "SPT" "I3.csv" |> allPeaks 9.0 |> Math.Matrix.map (fun x -> if x = 0.0 then 0.0 else 1.0)
    let peaksI3m = loadTestFile2 "SPT" "peaksI3.csv" 
    Assert.True (matrixFloatEquals peaksI3m peaksI3 0.0001)
    
[<Fact>]
let testRemoveSmall () =
    let peaksI3_2 = loadTestFile2 "SPT" "peaksI3.csv" |> removeSmall 15
    let peaksI3_2m = loadTestFile2 "SPT" "peaksI3_2.csv"
    Assert.True (matrixFloatEquals peaksI3_2m peaksI3_2 0.0001)
    
[<Fact>]
let testDilate () =
    let m = loadTestFile2 "SPT" "I3.csv"
    let peaksI3_3 = loadTestFile2 "SPT" "peaksI3_2.csv" |> dilate 9.0 3 m
    let peaksI3_3m = loadTestFile2 "SPT" "peaksI3_3.csv"
    Assert.True (matrixFloatEquals peaksI3_3m peaksI3_3 0.0001)