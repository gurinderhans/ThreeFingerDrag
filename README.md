# ThreeFingerDrag

### This project imitates macOS three finger drag experience on Windows trackpads
- You can initiate a drag by just dragging three fingers on your trackpad.
- When your fingers reach the edge of the trackpad, <ins>*lift and re-place three fingers*</ins> in center to continue dragging.
- The drag ends, immediately, if you start another gesture, or after few seconds, when your fingers leave the trackpad.
- This allows dragging a file or window with just one hand.

## Building code 🛠️
1. Open `src/tfd.sln` in Visual Studio.
2. Press **Ctrl+B / Ctrl+Shift+B** to build code.

## How do I run the app?
Download and run `tfd.exe` or build the code and run the app.

### Notes:
- You'll also need to disable other three finger gestures in windows trackpad settings
    <img width="555" alt="paint-clip" src="https://user-images.githubusercontent.com/3400083/204067942-55fb5923-6101-4580-8916-35dce32fbe6a.png" />

- You'll need to run `tfd.exe` as administrator, if you want to drag other programs that run as administrator (ex. Task Manager)

    <img src="https://user-images.githubusercontent.com/3400083/203868101-65520672-44e3-4e50-a69c-15585f23da7b.png" />


## Customize Settings 
You can set various environment variables to change drag behavior (ex. drag speed multiplier, velocity).

The easiest way to configure this is using powershell script that runs `tfd.exe` app.

Ex. Create powershell script named `tfd.ps1` with below contents:
```
# auto run as admin, if started from non-admin shell
if (-not(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator")))
{
    $args = "& '$($MyInvocation.MyCommand.Definition)'";
    Start-Process powershell -Verb:runAs -ArgumentList:$args;
    return;
}

# when script re-runs as admin, it'll run this part
$env:DragSpeedMultiplier=1.5;
$env:DragEndConfidenceThreshold=5;
$env:DragStartFingersApartDistThreshold=2.6;

& "/yourPathTo/tfd.exe";
```

## Env variables
Below env. variables have pre-set values, but you can override based on your needs.
#### 1. `DragSpeedMultiplier`
- The higher the value, the farther cursor will move for some trackpad movement with your fingers (*think mouse speed slider in windows*).
#### 2. `DragEndConfidenceThreshold`
- If drag is ending too often as you 'lift and re-place' three fingers on trackpad, increase this value.
#### 3. `DragVelocityUpperBoundX` & `DragVelocityUpperBoundY`
- The faster your fingers move on trackpad, the faster the cursor will move. These values define **upper** bounds for cursor velocity in X/Y direction.
- Set both to `1` if you want **linear cursor movement** and `>1` for dynamic cursor speed.
#### 4. `DragEndMillisecondsThreshold`
- The number of milliseconds to wait before drag is ended, if three fingers are **NOT** touching trackpad.
#### 5. `DragStartFingersApartDistThreshold`
- Ensures drag starts only if three finger contacts are approximately close to each other.
- The distance between three contacts is calculated, and `if largest_dist < (smallest_dist x this_var)`, then drag is initiated.
