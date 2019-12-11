We have been prototyping a callgraph-based analyzer for the linker, that reports "linker-unsafe" usage in the IL reachable from some set of entry points (Main, or all public surface area). It simulates attributes that influence how unsafe patterns get reported.

To give it a try:
```
> git clone https://github.com/vitek-karas/linker -b Analysis
> cd linker
linker> ./eng/dotnet.{sh/ps1} build illink.sln
```

Run the linker specifying the desired set of roots, optimizations, etc. A summary of the analysis will be written to stdout, and the "callstacks" will be written to `trimanalysis.json` in the linker output directory.
For example: 

```
linker> dotnet ./src/ILLink.Tasks/bin/Debug/netcoreapp2.0/illink.dll -a console -c link -u link -d testapps/console/out/ --skip-unresolved true --ignore-descriptors true --verbose --dump-dependencies --used-attrs-only true -out testapps/console/linked --analyze-for-trim
```

Here, the command-line flags have the following meaning.
```
-a console:                    Roots console.dll. The linker will see that it is an application, and root only the entry point (not all public APIs)
-c link -u link:               Tells the linker to trim all application (-u) and framework (-c) libraries unless otherwise specified.
-d testapps/console/out:       Tells the linker where to find the dependencies of the application.
--skip-unresolved:             Continue when encountering unresolved dependencies.
--ignore-descriptors true:     Don't root types/members specified in embedded .xml descriptors. Use this option to see only what is kept by the linker's analysis.
--verbose:                     Output extra info to stdout, such as the methods with unanalyzed reflection patterns.
--used-attrs-only true:        Only keep custom attributes when the attribute type is referenced. (Otherwise this keeps all custom attributes on kept types).
--out testapps/console/linked: Output the linked app into the specified directory.
```
All of the above are standard linker flags. To enable the analyzer:
```
--analyze-for-trim:            Output a summary of the analysis to console, and write trimanalysis.json into the linker output (--out) directory.
```
