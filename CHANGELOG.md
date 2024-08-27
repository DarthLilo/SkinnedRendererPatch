# 1.1.3
- Removed some transpiler code which was causing compatability errors, this may result in false positive errors for grabbable objects during scrap spawning, these are safe to ignore.
- Cleaned up some extra logging

# 1.1.2
- Fixed an issue with loading empty save data

# 1.1.1
- Rewrote how the soft dependency was loaded to be more in line with standards

# 1.1.0
- Fixed item state desync errors when when loading items that weren't in the save file

- Items now maintain their state across save reloads. (for example all triangle flasks would become round flasks)

- Items now properly sync across multiple clients

- Added specific support for Lilo's Scrap Extension for tamed animal states.

# 1.0.0
- Initial Release