
dotnet build
mkdir -p package
cp "CustomHitSound/bin/Debug/CustomHitSound.dll" package
cp Info.json "CustomHitSound/bin/Debug/LightJson.dll" package
cd "F:/Steam/steamapps/common/A Dance of Fire and Ice/Mods"
mkdir -p CustomHitSound
rm CustomHitSound/*.cache
cd $OLDPWD
cp package/* "F:/Steam/steamapps/common/A Dance of Fire and Ice/Mods/CustomHitSound"
"F:\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice.exe"