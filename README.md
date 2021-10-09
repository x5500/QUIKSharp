QUIKSharp#
==========
QUIKSharp is the QUIK Lua interface ported to .NET.
The library exposes most functions and events available in QLUA as C# async functions and events.
This is a fork of the original library, radically changed.

CHANGES: PERFOMANCE+++
==========
 + Changed API.
 + The API has been changed.
 + Some services have been rewritten
 + Rewritten workflow of transactions and TransID.
 + Added Batch processing for some common used functions like: GetParamEx
 + Rewitten Transac & TransID workflow
 + JSON encoder/decoder powered up with custom BASE64 serialize for int64 & DOUBLE values.
 + LUA JSON encoder/decoder speedup with custom BASE64 serialize for int64 & DOUBLE values.
 - Candles Service got no improvements, they are still as slow as they were...

License
----------------------
Original QUIKSharp library Licensed under the Apache License, Version 2.0 (the "License").
Some components are licensed under MIT License.

This software is distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
