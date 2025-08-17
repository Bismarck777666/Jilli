@echo off
setlocal enabledelayedexpansion

for /r %%F in (*.webp) do (
    set "webp=%%F"
    set "basename=%%~nF"
    set "folder=%%~dpF"
    set "png=%%~dpF%%~nF.png"
    set "astc=%%~dpF%%~nF.astc"

    if not exist "!astc!" (
        echo [*] Converting !webp! to !png!
        dwebp "!webp!" -o "!png!"

        echo [*] Encoding !png! to !astc!
        astcenc-avx2.exe -cl "!png!" "!astc!" 6x6 -medium

        echo [*] Deleting temporary PNG file !png!
        del "!png!"
    ) else (
        echo [=] Skipping: !astc! already exists.
    )
)

echo.
echo All done.
pause
