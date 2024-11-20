#! /bin/bash

if [[ $* == *--bos* ]] then
    konsole --new-tab -e dotnet run --project ../behide-oneline-services/src/Behide.OnlineServices
fi

if [[ $* == *--pane* ]] then
    echo '{
        "Orientation": "Vertical",
        "Widgets": [
            { "SessionRestoreId": 0 },
            { "SessionRestoreId": 0 }
        ]
    }' > /tmp/behide-godot-konsole-layout.json
    konsole --new-tab --layout /tmp/behide-godot-konsole-layout.json &
    sleep 1

    service="$(qdbus | grep -B1 konsole | grep -v -- -- | sort -t"." -k2 -n | tail -n 1)"

    qdbus $service /Sessions/1 org.kde.konsole.Session.runCommand "godot -d -- --log-directly-to-console"
    qdbus $service /Sessions/2 org.kde.konsole.Session.runCommand "godot -d -- --log-directly-to-console"
else
    konsole --new-tab -e godot -d -- --log-directly-to-console
    konsole --new-tab -e godot -d -- --log-directly-to-console
fi
