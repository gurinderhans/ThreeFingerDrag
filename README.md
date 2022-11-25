# ThreeFingerDrag

**This project imitates macOS three finger drag experience on Windows trackpads.**

#### More specifically,
- You can start drag (ie. **'mouse click down + drag'**) by just dragging three fingers over your trackpad.
- When your fingers reach end of trackpad, **lift and re-place three fingers** elsewhere to continue dragging.
- The drag ends (ie. **'mouse click up'**), immediately, if you start another gesture, or lift fingers from trackpad for an extended time.
- This allows select-dragging with only one hand, and virtually unlimited trackpad space.

## Building the code
1. Open `src/tfd.sln`
2. Build away! (Ctrl+B / Ctrl+Shift+B)

## How do I run it?
#### Download and run `tfd.exe`, *"It just works."*
- But on a serious note, there are settings you can configure to make your experience more comfortable. So keep reading...

- **You'll also need to disable other three finger gestures in windows trackpad settings.**

    <img width="555" alt="paint-clip" src="https://user-images.githubusercontent.com/3400083/204067942-55fb5923-6101-4580-8916-35dce32fbe6a.png">

- **You'll need to run `tfd.exe` as administrator, if you want to drag other programs that run as administrator.**

    ![image](https://user-images.githubusercontent.com/3400083/203868101-65520672-44e3-4e50-a69c-15585f23da7b.png)


## Customize settings
You can set environment variables to change drag behavior in various ways (ex. drag speed multiplier, velocity, etc.).

The simplest way to achieve this is with a powershell script.

Ex. Create file named `tfd.ps1` with below contents:
```
# auto run as admin, if started from non-admin shell
if (-not(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator")))
{
    $args = "& '$($MyInvocation.MyCommand.Definition)'";
    Start-Process powershell -Verb:runAs -ArgumentList:$args;
    return;
}

# when script re-runs as admin, it'll run this part
$env:DragSpeedMultiplier=1.1;
$env:DragEndConfidenceThreshold=4;
$env:DragStartFingersApartDistThreshold=2.5;
$env:DragVelocityMaxThreshold=3.5;
$env:DragEndMillisecondsThreshold=700;

& "/yourPathTo/tfd.exe";
```

## Env variables
Below subset of env. variables have a default value, but you can override based on your hardware/needs.
#### 1. `DragSpeedMultiplier`
- The higher the value, the farther cursor will move for some trackpad movement (*think mouse speed slider in windows*).
#### 2. `DragEndConfidenceThreshold`
- If drag is ending too often as you 'lift and re-place' three fingers on trackpad, increment this value.
#### 3. `DragVelocityMinThreshold` & `DragVelocityMaxThreshold`
- The faster your fingers move on trackpad, the faster the cursor will move. These values define **lower** and **upper** bounds for cursor speed.
- Set both to `1` if you want **linear cursor movement**.
#### 4. `DragEndMillisecondsThreshold`
- The number of milliseconds to wait before drag is ended, if three fingers are NOT touching trackpad.
#### 5. `DragStartFingersApartDistThreshold`
- Ensures drag starts only if three finger contacts are approximately close to each other.
- The distance between three contacts is calculated, and `if largest_dist < (smallest_dist x this_var)`, then drag is initiated.
