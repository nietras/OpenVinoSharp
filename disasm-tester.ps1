param(
    [string]$runtime = "win-arm64"
)
dotnet publish src/OpenVinoSharp.Tester/OpenVinoSharp.Tester.csproj -c Release -r "$runtime" -f net10.0 --self-contained true /p:PublishAot=true /p:DebugSymbols=true
dumpbin /DISASM /SYMBOLS "artifacts\publish\OpenVinoSharp.Tester\release_$runtime\OpenVinoSharp.Tester.exe" > "artifacts\publish\OpenVinoSharp.Tester\release_$runtime\disassembly.asm"
