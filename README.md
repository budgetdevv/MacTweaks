# MacTweaks
A program that enable tweaks I'm making to my mac

This project is not really ready for public consumption atm, as there are loads of things specific to my device ( E.x. DLLImport path )

# Why should you use MacTweaks
- Some features are not provided by most apps in the App Store. In fact, I started this project because I required a feature that only a single app provided, and the app cost 20 bucks
- Native apple silicon support
- High performance and low memory footprint

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
- Auto bypass "Enter admin password" dialog!!!

# Some gotchas
- Does not support intel-based CPUs YET ( I will have to compile dynlib for intel-based )
- No support for disabling modules YET
