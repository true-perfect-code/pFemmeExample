%% https://mermaid.live/

graph LR
    Start((App Start)) --> Init[AppInitialize Service]
    Init --> Check{Token valide?}
    Check -- Ja --> UI[UI rendern / Home]
    Check -- Nein --> Choice{Local Mode erlaubt?}
    Choice -- Ja --> Local[Nur lokale Daten nutzen]
    Choice -- Nein --> Logout[Erzwinge Neuanmeldung]