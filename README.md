# MacTweaks
A program that enable tweaks I'm making to my mac

This project is not really ready for public consumption atm, as there are loads of things specific to my device ( E.x. DLLImport path )

# Why should you use MacTweaks
- Some features are not provided by most apps in the App Store. In fact, I started this project because I required a feature that only a single app provided, and the app cost 20 bucks
- Native apple silicon support
- High performance and low memory footprint

# Setup

MacTweaks relies on the terminal for sudo elevation via shell-less execution. However, this prompts for a password in the console dialog, which is inaccessible when running shell-less.

While MacTweaks could request the user's password in a dialog, it wouldn't be a native macOS dialog, and entering a password every time MacTweaks is launched can be cumbersome.

The ideal solution is to enable Touch ID for Terminal sudo elevation. Follow these steps (as shown in the screenshot) to configure this feature:

**1. Ensure Touch ID is Enabled for `sudo`:**

Check if Touch ID is configured for `sudo` by editing the `/etc/pam.d/sudo` file.

1. Open the terminal and run:
   ```bash
   sudo nano /etc/pam.d/sudo
   ```

2. Add the following line at the top (if it’s not already there):
   ```bash
   auth       sufficient     pam_tid.so
   ```

   This setting allows macOS to use Touch ID for `sudo` commands.

3. Save the file (`Ctrl + O`, then `Enter`) and exit (`Ctrl + X`).

Once set up, users can choose to use Touch ID or enter their password for sudo elevation. Note that this process needs to be repeated after installing a new version of macOS.

# Feature List
- Click on dock icon to hide app
- Click on bottom right corner to hide all apps
- Click on window's red cross to quit application entirely ( Should there be 1 window left )
- `Command+Q` to close Finder window
- `Command+Delete` to eject volumes
- `Command+X` to cut files and folders
- Retain brightness when toggling low power mode ( ON / OFF )
- Reset low power config to "Low power mode on battery only" when plugged back into AC. This is useful when you wish to override power settings to use high performance mode for certain workloads while running on battery
- Mouse buttons for Finder navigation. Holding `Shift` key while navigating back causes finder to navigate to parent directory instead
- Application blacklist ( Force terminates an application that is blacklisted on launch )
- Auto bypass "Enter admin password" dialog!!!

# Some gotchas
- Does not support intel-based CPUs YET ( I will have to compile dynlib for intel-based )
- No support for disabling modules YET
