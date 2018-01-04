## Directory Overview

* `Demos`: Contains a Project that demonstrates the usage of important parts of LamestWebserver. The code in here should be very descriptive or self explanatory. Basic topics should even have comments explaning them.
* `LamestWebserver`: Contains the core code of the LamestWebserver Framework & Libraries.
* `UnitTests`: Contatins the unit tests for LamestWebserver Libraries.
* `content`: Contains images & files for the `README.md` visual presentation.
* `dependencies`: Contains the projects, LamestWebserver depends on that can't be included as a nuget package: "sha3" (An open source SHA3 Hashing Library) and "ILMerge" (An open source .Net Assembly Merger used in the build pipeline).
* `lwshostcore`: Contains the core features for executing hosted LamestWebserver applications.
* `lwshostsvc`: Contains the Windows Service for executing hosted LamestWebserver applications.


## Dependencies

### Demos
Depends on LamestWebserver.

### LamestWebserver
Depends on Fleck, Newtonsoft.Json, sha3 and ILMerge if you want the assemblies to be merged.

### UnitTests
Depends on LamestWebserver.

### lwshostcore
Depends on LamestWebserver.

### lwshostsvc
Depends on LamestWebserver and lwshostcore.

<h1 align="center">
  <br><br>
  <img src="https://raw.githubusercontent.com/rainerzufalldererste/LamestWebserver/master/LamestWebserver/content/lwsbubbles.png" style="width: 700px; max-width: 80%">
</h1>