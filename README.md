# tds2scs
Convert all your Sitecore TDS projects in your solution to SCS module files

## What is tds2scs?

This is a throw away C# console app (written in .NET Core 3.1) that I've recently written to take the burnden off filtering through massive amounts of TDS projects that may exist in your Sitecore solution. It doesn't do everything, but it will generate a base .module.json file for you to modify to your needs. It's best suited to helix designed systems where items are seralised in TDS as example, Feature/{Module}. If it doesn't match your setup, feel free to modify it to your needs!

As this is only really a once run script, you'll have to excuse my simplistic approach. It will also log the items it didn't know how to handle properly.

I've blogged about it in [this post](https://spyn.me/sitecore-migrating-from-tds-to-scs/).

## Usage
1. Build the application, I won't be supplying binaries
2. Copy appsettings.example.json to appsettings.json
3. Edit appsettings.json to your needs
4. Run the executable! (Note: this WILL overwrite any existing module json files with the same name)
5. Review the /logs - this will tell you the items it didn't know how to handle, and you can adjust accordingly
6. Review the .module.json files
7. Sync your life away

```
> tds2scs.exe <solution name>
```

This will generate a file two folders down from the TDS project.
```
../../<TDS Project Name>.module.json
```

You will have to run the syncronisation in SCS in Visual Studio to generate the items.

## Settings in appsettings.json

```
{
  "helixModule": "<Name of your 'helix module', eg HBF>",
  "maxRelativePathLength": 100,
  "solutionFile": "<Path of your solution file, if you want>",
  "serialisationFolder": "<Folder where to seralise your data, empty to use default>"
}
```

## Other notes

My module globs in SCS were set to 
```
src/*/*/* 
```
YMMV

## Future plans?
Maybe, if people use it I could look at integrating it into the Sitecore CLI! Or if anyone else wants to, go right ahead. This _is_ published under the MIT license.

## Think this script is really useful?!
If you found this script useful, feel free to [donate me](https://www.paypal.com/donate/?business=2D4X8BNXQ2FVW&no_recurring=1&item_name=Just+doing+dev+things&currency_code=AUD) a beer :) it helps makes me do neat things like this

or BTC to 17WQoBHboiPuogJptEyGP4CtfSw1sks3ap
