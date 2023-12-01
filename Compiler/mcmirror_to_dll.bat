@rem Compiles ./MCMirror/* to ./MCMirror.dll and ./MCMirror.xml.

@rem The setup of the `MCMirror` directory is jank to such a degree that VS
@rem doesn't let me compile it. As such, a script to do it manually.

@rem Yeah I know, using bat in 2023, but eh.

@rem TODO: At some point link the version to the relevant commit.
@rem (Unfortunately needs to be a valid numeric x.y.z.w. Lame.)

dotnet build .\MCMirror\MCMirror.csproj --no-dependencies --no-incremental --no-self-contained --property:OutputPath=.\build-temp;GenerateDocumentationFile=true --property:Version=0.0.0.1 && (
    del .\MCMirror.dll
    del .\MCMirror.xml
    copy /Y .\MCMirror\build-temp\MCMirror.dll .\MCMirror.dll
    copy /Y .\MCMirror\build-temp\MCMirror.xml .\MCMirror.xml
    del .\MCMirror\build-temp\MCMirror.dll
    del .\MCMirror\build-temp\MCMirror.xml
    del .\MCMirror\build-temp\MCMirror.deps.json
    rmdir .\MCMirror\build-temp
    echo(
    echo(
    echo Compilation succesful!
) || (
    echo(
    echo(
    echo Compilation failed!
)

@rem To use it in VS, right click the project, click "Add Project Reference",
@rem click the "Browse" button in the bottom-right, and grab the MCMirror.dll.
@rem Make sure that you do NOT include other references. This causes stuff to
@rem fail.

@rem To have the proper documentation, make sure you have the MCMirror.xml file
@rem next to the dll file.

@rem TODO: Add a sample .csproj that handles all reference stuff for you.