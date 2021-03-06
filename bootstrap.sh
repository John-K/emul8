#!/bin/bash
#
# ==========================
# Emul8 bootstrapping script
# ==========================
#
# This script is used to create the <<Emul8.sln>> file
# with references to the selected modules:
#
# * UI module
# * extension libraries with peripherals
# * plugins
# * tests
# * other projects

set -e

BATCH_MODE=0
OUTPUT_DIRECTORY="target"
BINARIES_DIRECTORY="bin"

while getopts "ad:o:b:h" opt
do
    case "$opt" in
        a)
            BATCH_MODE=1
            ;;
        d)
            DIRECTORY="$OPTARG"
            ;;
        o)
            OUTPUT_DIRECTORY="$OPTARG"
            ;;
        b)
            BINARIES_DIRECTORY="$OPTARG"
            ;;
        h)
            echo "Emul8 bootstrapping script"
            echo "=========================="
            echo "Usage: $0 [-a] [-d directory] [-h]"
            echo "  -a            batch mode, generates the 'All projects' solution without"
            echo "                any interaction with the user"
            echo "  -d directory  location of the base directory to scan"
            echo "  -b directory  location for binaries created from generated project"
            echo "  -o directory  location of generated project files"
            echo "  -h            prints this help"
            exit 0
    esac
done

if ! [ -x "$(command -v mono)" ]
then
    echo "Mono not found. Please refer to documentation for installation instructions. Exiting!"
    exit 1
fi

# Check mono version
MONO_VERSION=`mono --version | sed -n -r 's/.* version ([0-9.]+) \(.*/\1/p'`
MONO_VERSION_MAJOR=`echo $MONO_VERSION | sed -n 's/[^0-9]*\([0-9]*\).*/\1/p'`
if [ $MONO_VERSION_MAJOR -lt 4 -a $MONO_VERSION != "3.99.0" ]
then
    echo "Wrong mono version detected: $MONO_VERSION. Please refer to documentation for installation instructions. Exiting!"
    exit 1
fi

git submodule update --init --recursive

if [ -z "$ROOT_PATH" ]; then
    # this is to support running emul8 from external directory
    ROOT_PATH="`dirname \`readlink -f $0\``"
fi

# Create Sandbox project
pushd $ROOT_PATH/Misc/Sandbox > /dev/null
if [ ! -e Sandbox.csproj ]
then
  echo " >> Creating Sandbox.csproj..."
  cp Sandbox.csproj-template Sandbox.csproj
  cp SandboxMain.cs-template SandboxMain.cs
fi
popd > /dev/null

BOOTSTRAPER_DIR=$ROOT_PATH/Tools/Bootstrap
BOOTSTRAPER_BIN=$BOOTSTRAPER_DIR/bin/Release/Bootstrap.exe

CCTASK_DIR=$ROOT_PATH/External/cctask
CCTASK_BIN=$CCTASK_DIR/CCTask/bin/Release/CCTask.dll

# We build bootstrap/cctask every time in order to have the newest versions at every bootstrapping.
xbuild $BOOTSTRAPER_DIR/Bootstrap.csproj /p:Configuration=Release /nologo /verbosity:quiet || (echo "There was an error during Bootstrap compilation!" && exit 1)
xbuild $CCTASK_DIR/CCTask.sln /p:Configuration=Release /nologo /verbosity:quiet            || (echo "There was an error during CCTask compilation!"    && exit 1)

if [ $BATCH_MODE -eq 1 ]
then
    mono $BOOTSTRAPER_BIN GenerateAll --directories "${DIRECTORY:-.}" --output-directory "$OUTPUT_DIRECTORY" --binaries-directory "$BINARIES_DIRECTORY"
else
    set +e
    mono $BOOTSTRAPER_BIN --interactive --directories "$DIRECTORY" --output-directory "$OUTPUT_DIRECTORY" --binaries-directory "$BINARIES_DIRECTORY"
    result=$?
    set -e
    clear
    case $result in
        0) echo "Solution file generated in $OUTPUT_DIRECTORY/Emul8.sln. Now you can run ./build.sh" ;;
        1) echo "Solution file generation cancelled." ;;
        2) echo "There was an error while generating the solution file." ;;
        3) echo "Bootstrap setup cleaned." ;;
    esac
    exit $result
fi
