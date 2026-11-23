# Run once on your PC (from the desktop folder): creates secrets\data.key (never committed).
# Then copy its contents into the GitHub secret STUDIODESK_DATA_KEY. Keep a backup somewhere safe:
# if the key is lost, new builds can't read Data packs from old builds (just rebuild everything).
dotnet run --project tools/StudioDesk.Packer -c Release -- new-key secrets/data.key
