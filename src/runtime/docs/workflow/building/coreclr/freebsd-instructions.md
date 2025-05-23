# Build CoreCLR on FreeBSD

* [Build using Docker](#build-using-docker)
* [Build using cross-compilation on Linux](#build-using-cross-compilation-on-linux)
* [Build directly on FreeBSD](#build-directly-on-freebsd)
* [Old Documentation](#old-documentation)
  * [Environment](#environment)
    * [Toolchain Setup](#toolchain-setup)
  * [Debugging CoreCLR (Optional)](#debugging-coreclr-optional)
  * [Git Setup](#git-setup)
  * [Build the Runtime](#build-the-runtime)
  * [Build the Framework Native Components](#build-the-framework-native-components)
  * [Build the Framework Managed Components](#build-the-framework-managed-components)
  * [Download Dependencies](#download-dependencies)
  * [Install Mono](#install-mono)
  * [Download the NuGet Client](#download-the-nuget-client)
  * [Download NuGet Packages](#download-nuget-packages)
  * [Compile an App](#compile-an-app)
  * [Run your App](#run-your-app)
  * [Run the test suite](#run-the-test-suite)
  * [Note on Clang/LLVM versions](#note-on-clangllvm-versions)

This guide will walk you through building CoreCLR on FreeBSD.

As mentioned in the [FreeBSD requirements doc](/docs/workflow/requirements/freebsd-requirements.md), there are three ways to go on about to build CoreCLR for FreeBSD:

* Build using Docker
* Build using cross-compilation on your own Linux environment
* Build directly on FreeBSD

## Build using Docker

Building for FreeBSD with Docker follows a very similar workflow to using Docker for Linux. Since this is also a cross-building scenario, the instructions are found in the [Docker section of the cross-building doc](/docs/workflow/building/coreclr/cross-building.md#cross-compiling-for-freebsd-with-docker).

## Build using cross-compilation on Linux

Ensure you have all of the prerequisites installed from the [Linux Requirements](/docs/workflow/requirements/linux-requirements.md), and the additional ones listed in the [FreeBSD Requirements](/docs/workflow/requirements/freebsd-requirements.md#linux-environment).

Once that is done, refer to the [Linux section of the cross-building doc](/docs/workflow/building/coreclr/cross-building.md#linux-cross-building). There are detailed instructions on how to cross-compile using your Linux environment, including a section dedicated to FreeBSD building.

You'll also need to use the `--bootstrap` option as documented in the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#building-coreclr-with-bootstrapping) to build the cross-compilation toolchain.

## Build directly on FreeBSD

Ensure you have all of the prerequisites installed from the [FreeBSD Requirements](/docs/workflow/requirements/freebsd-requirements.md).

Instructions for building directly on FreeBSD coming soon!

Meanwhile, here are the old instructions.

## Old Documentation

These instructions were written quite a while ago, and they may or may not work today. Updated instructions coming soon.

### Environment

These instructions assume you use the binary package tool `pkg` (analog to `apt-get` or `yum` on Linux) to install the environment. Compiling the dependencies from source using the ports tree might work too, but is untested.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 4069](https://github.com/dotnet/runtime/issues/4069)).

#### Toolchain Setup

Install the following packages for the toolchain:

- bash
- cmake
- llvm37 (includes LLVM 3.7, Clang 3.7 and LLDB 3.7)
- libunwind
- gettext
- icu
- ninja (optional)
- lttng-ust
- python27

To install the packages you need:

```sh
janhenke@freebsd-frankfurt:~ % sudo pkg install bash cmake libunwind gettext llvm37 icu
```

The command above will install Clang and LLVM 3.7. For information on building CoreCLR with other versions, see section on [Clang/LLVM versions](#note-on-clangllvm-versions).

### Debugging CoreCLR (Optional)

Note: This step is not required to build CoreCLR itself. If you intend on hacking or debugging the CoreCLR source code, you need to follow these steps. You must follow these steps *before* starting the build itself.

In order to debug CoreCLR you will also need to install [LLDB](http://lldb.llvm.org/), the LLVM debugger.

To build with clang 3.7 from coreclr project root:

```sh
LLDB_LIB_DIR=/usr/local/llvm37/lib LLDB_INCLUDE_DIR=/usr/local/llvm37/include ./build.sh clang3.7 debug
```

Run tests:

```sh
./src/pal/tests/palsuite/runpaltests.sh $PWD/artifacts/obj/FreeBSD.x64.Debug $PWD/artifacts/paltestout
```

### Git Setup

This guide assumes that you've cloned the corefx and coreclr repositories into `~/git/corefx` and `~/git/coreclr` on your FreeBSD machine and the corefx and coreclr repositories into `D:\git\corefx` and `D:\git\coreclr` on Windows. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in on both the FreeBSD and Windows machine.

### Build the Runtime

To build the runtime on FreeBSD, run build.sh from the root of the coreclr repository:

```sh
janhenke@freebsd-frankfurt:~/git/coreclr % ./build.sh
```

Note: FreeBSD 10.1-RELEASE system's Clang/LLVM is 3.4, the minimum version to compile CoreCLR runtime is 3.5. See [Note on Clang/LLVM versions](#note-on-clangllvm-versions).

If the build fails with errors about resolving LLVM-components, the default Clang-version assumed (3.5) may not be appropriate for your system.
Override it using the following syntax. In this example LLVM 3.6 is used:

```sh
janhenke@freebsd-frankfurt:~/git/coreclr % ./build.sh clang3.6
```


After the build is completed, there should some files placed in `artifacts/Product/FreeBSD.x64.Debug`.  The ones we are interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `libcoreclrpal.so`: The platform abstraction library for the CoreCLR runtime. This library is temporary and the functionality will be merged back into `libcoreclr.so`

In order to keep everything tidy, let's create a new directory for the runtime and copy the runtime and corerun into it.

```sh
janhenke@freebsd-frankfurt:~/git/coreclr % mkdir -p ~/coreclr-demo/runtime
janhenke@freebsd-frankfurt:~/git/coreclr % cp artifacts/Product/FreeBSD.x64.Debug/corerun ~/coreclr-demo/runtime
janhenke@freebsd-frankfurt:~/git/coreclr % cp artifacts/Product/FreeBSD.x64.Debug/libcoreclr*.so ~/coreclr-demo/runtime
```

### Build the Framework Native Components

```sh
janhenke@freebsd-frankfurt:~/git/corefx$ ./build-native.sh
janhenke@freebsd-frankfurt:~/git/corefx$ cp artifacts/FreeBSD.x64.Debug/Native/*.so ~/coreclr-demo/runtime
```

### Build the Framework Managed Components

We don't _yet_ have support for building managed code on FreeBSD, so you'll need a Windows machine with clones of both the CoreCLR and CoreFX projects.

You will build `System.Private.CoreLib.dll` out of the coreclr repository and the rest of the framework that out of the corefx repository.  For System.Private.CoreLib (from a regular command prompt window) run:

```
D:\git\coreclr> build.cmd freebsdmscorlib
```

The output is placed in `bin\Product\FreeBSD.x64.Debug\System.Private.CoreLib.dll`.  You'll want to copy this to the runtime folder on your FreeBSD machine. (e.g. `~/coreclr-demo/runtime`)

For the rest of the framework, you need to pass some special parameters to build.cmd when building out of the CoreFX repository.

```
D:\git\corefx> build-managed.cmd -os=Linux -target-os=Linux -SkipTests
```

Note: We are using the Linux build currently, as CoreFX does not yet know about FreeBSD.

It's also possible to add `/t:rebuild` to the build.cmd to force it to delete the previously built assemblies.

For the purposes of Hello World, you need to copy over both `bin\Linux.AnyCPU.Debug\System.Console\System.Console.dll` and `bin\Linux.AnyCPU.Debug\System.Diagnostics.Debug\System.Diagnostics.Debug.dll`  into the runtime folder on FreeBSD. (e.g `~/coreclr-demo/runtime`).

After you've done these steps, the runtime directory on FreeBSD should look like this:

```
janhenke@freebsd-frankfurt:~/git/coreclr % ls ~/coreclr-demo/runtime/
System.Console.dll  System.Diagnostics.Debug.dll  corerun  libcoreclr.so  libcoreclrpal.so  System.Private.CoreLib.dll
```

### Download Dependencies

The rest of the assemblies you need to run are presently just facades that point to System.Private.CoreLib.  We can pull these dependencies down via NuGet (which currently requires Mono).

Create a folder for the packages:

```sh
janhenke@freebsd-frankfurt:~/git/coreclr % mkdir ~/coreclr-demo/packages
janhenke@freebsd-frankfurt:~/git/coreclr % cd ~/coreclr-demo/packages
```

### Install Mono

If you don't already have Mono installed on your system, use the pkg tool again:

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % sudo pkg install mono
```

### Download the NuGet Client

Grab NuGet (if you don't have it already)

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % curl -L -O https://nuget.org/nuget.exe
```
### Download NuGet Packages

With Mono and NuGet in hand, you can use NuGet to get the required dependencies.

Make a `packages.config` file with the following text. These are the required dependencies of this particular app. Different apps will have different dependencies and require a different `packages.config` - see [Issue #4053](https://github.com/dotnet/runtime/issues/4053).

```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="System.Console" version="4.0.0-beta-22703" />
  <package id="System.Diagnostics.Contracts" version="4.0.0-beta-22703" />
  <package id="System.Diagnostics.Debug" version="4.0.10-beta-22703" />
  <package id="System.Diagnostics.Tools" version="4.0.0-beta-22703" />
  <package id="System.Globalization" version="4.0.10-beta-22703" />
  <package id="System.IO" version="4.0.10-beta-22703" />
  <package id="System.IO.FileSystem.Primitives" version="4.0.0-beta-22703" />
  <package id="System.Reflection" version="4.0.10-beta-22703" />
  <package id="System.Resources.ResourceManager" version="4.0.0-beta-22703" />
  <package id="System.Runtime" version="4.0.20-beta-22703" />
  <package id="System.Runtime.Extensions" version="4.0.10-beta-22703" />
  <package id="System.Runtime.Handles" version="4.0.0-beta-22703" />
  <package id="System.Runtime.InteropServices" version="4.0.20-beta-22703" />
  <package id="System.Text.Encoding" version="4.0.10-beta-22703" />
  <package id="System.Text.Encoding.Extensions" version="4.0.10-beta-22703" />
  <package id="System.Threading" version="4.0.10-beta-22703" />
  <package id="System.Threading.Tasks" version="4.0.10-beta-22703" />
</packages>

```

And restore your packages.config file:

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % mono nuget.exe restore -Source https://www.myget.org/F/dotnet-corefx/ -PackagesDirectory .
```

NOTE: This assumes you already installed the default CA certs. If you have problems downloading the packages please see [Issue #4089](https://github.com/dotnet/runtime/issues/4089#issuecomment-88203778). The command for FreeBSD is:

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % mozroots --import --sync
```

Finally, you need to copy over the assemblies to the runtime folder.  You don't want to copy over System.Console.dll or System.Diagnostics.Debug however, since the version from NuGet is the Windows version.  The easiest way to do this is with a little find magic:

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % find . -wholename '*/aspnetcore50/*.dll' -exec cp -n {} ~/coreclr-demo/runtime \;
```

### Compile an App

Now you need a Hello World application to run.  You can write your own, if you'd like.  Personally, I'm partial to the one on corefxlab which will draw Tux for us.

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % cd ~/coreclr-demo/runtime
janhenke@freebsd-frankfurt:~/coreclr-demo/runtime % curl -O https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs
```

Then you just need to build it, with `mcs`, the Mono C# compiler. FYI: The Roslyn C# compiler will soon be available on FreeBSD.  Because you need to compile the app against the .NET Core surface area, you need to pass references to the contract assemblies you restored using NuGet:

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/runtime % mcs /nostdlib /noconfig /r:../packages/System.Console.4.0.0-beta-22703/lib/contract/System.Console.dll /r:../packages/System.Runtime.4.0.20-beta-22703/lib/contract/System.Runtime.dll HelloWorld.cs
```

### Run your App

You're ready to run Hello World!  To do that, run corerun, passing the path to the managed exe, plus any arguments.  The HelloWorld from corefxlab will print a daemon if you pass "freebsd" as an argument, so:

```sh
janhenke@freebsd-frankfurt:~/coreclr-demo/runtime % ./corerun HelloWorld.exe freebsd
```

If all works, you should be greeted by a friendly daemon you know well.

Over time, this process will get easier. We will remove the dependency on having to compile managed code on Windows. For example, we are working to get our NuGet packages to include both the Windows and FreeBSD versions of an assembly, so you can simply nuget restore the dependencies.

A sample that builds Hello World on FreeBSD using the correct references but via XBuild or MonoDevelop would be great! Some of our processes (e.g. the System.Private.CoreLib build) rely on Windows-specific tools, but we want to figure out how to solve these problems for FreeBSD as well. There's still a lot of work ahead, so if you're interested in helping, we're ready for you!


### Run the test suite

If you've made changes to the CoreCLR PAL code, you might want to run the PAL tests directly to validate your changes.
This can be done after a clean build, without any other dependencies.

From the coreclr project directory:

```sh
janhenke@freebsd-frankfurt:~/coreclr % ./src/pal/tests/palsuite/runpaltests.sh  ~/coreclr/artifacts/obj/FreeBSD.x64.Debug ~/coreclr/artifacts/paltestout
```

This should run all the tests associated with the PAL.

### Note on Clang/LLVM versions

The minimum version to build CoreCLR is Clang 3.5 or above.

FreeBSD 10.X releases ship with Clang 3.4.

If you intend on building CoreCLR with LLDB debug support, pick llvm37 or llvm-devel.

To install clang 3.5: `sudo pkg install clang35`

To install clang 3.6: `sudo pkg install clang36`

To install clang 3.7: `sudo pkg install llvm37`

To install clang development snapshot: `sudo pkg install llvm-devel`

clang35 and clang36 download llvm35 and llvm36 packages as a dependency.

llvm37 and llvm-devel include clang and lldb. Since clang is included with llvm 3.7 and onward, there is no clang37 package.

After you have installed your desired version of LLVM you will need to specify the version to the build.sh script.

For example, if you chose to install llvm37 you would add the clangX.X to your build command as follows.
```sh
janhenke@freebsd-frankfurt:~/git/coreclr % ./build.sh clang3.7
```
