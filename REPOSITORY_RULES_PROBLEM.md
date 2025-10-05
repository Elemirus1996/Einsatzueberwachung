# Release-Prozess für Einsatzüberwachung Professional

## Problem: Repository Rules blockieren Tag-Push

Ihr GitHub Repository hat Repository Rules aktiviert, die das Pushen von Tags verhindern.

**Fehler:**
```
remote: error: GH013: Repository rule violations found for refs/tags/v1.7.1.
remote: - Cannot create ref due to creations being restricted.
```

## 🔧 Lösungen

### Option 1: Repository Rules anpassen (Empfohlen für automatische Releases)

1. **Öffnen Sie die Repository Rules:**
   - Gehen Sie zu: https://github.com/Elemirus1996/Einsatzueberwachung/settings/rules
   - Oder: Settings → Rules → Rulesets

2. **Finden Sie die blockierende Rule:**
   - Suchen Sie nach einer Regel, die Tags betrifft
   - Wahrscheinlich eine Regel die "Tag protection" oder "Ref creation" einschränkt

3. **Passen Sie die Regel an:**
   - **Option A:** Deaktivieren Sie die Tag-Einschränkung komplett
   - **Option B:** Fügen Sie eine Ausnahme hinzu für Tags die mit `v` beginnen
   - **Option C:** Fügen Sie sich selbst als "Bypass list" hinzu

4. **Speichern und erneut versuchen:**
   ```cmd
   Create-Tag-GitBash.bat
   ```

### Option 2: GitHub Web-Interface nutzen (Schnellste Lösung)

Da der lokale Tag bereits erstellt wurde, können Sie das Release direkt auf GitHub erstellen:

#### Schritt 1: Workflow manuell starten
1. Gehen Sie zu: https://github.com/Elemirus1996/Einsatzueberwachung/actions
2. Klicken Sie auf "Release" Workflow (links in der Liste)
3. Klicken Sie auf "Run workflow" (rechts oben)
4. Wählen Sie Branch: `master`
5. Klicken Sie "Run workflow"

**ABER:** Der Workflow erwartet einen Tag als Trigger. Daher:

#### Schritt 2: Release über GitHub UI erstellen
1. Gehen Sie zu: https://github.com/Elemirus1996/Einsatzueberwachung/releases
2. Klicken Sie auf "Draft a new release"
3. Klicken Sie auf "Choose a tag"
4. Geben Sie ein: `v1.7.1`
5. Klicken Sie "Create new tag: v1.7.1 on publish"
6. Titel: `Release v1.7.1`
7. Beschreibung: `Einsatzüberwachung Professional v1.7.1`
8. Klicken Sie "Publish release"

**Das erstellt den Tag UND startet automatisch den Workflow!**

### Option 3: Mit GitHub CLI (gh)

Wenn Sie GitHub CLI installiert haben:

```bash
# Authentifizieren
gh auth login

# Release mit Tag erstellen (startet Workflow automatisch)
gh release create v1.7.1 --title "Release v1.7.1" --notes "Einsatzüberwachung Professional v1.7.1"
```

### Option 4: Personal Access Token verwenden

Erstellen Sie einen Personal Access Token mit erweiterten Rechten:

1. Gehen Sie zu: https://github.com/settings/tokens
2. Klicken Sie "Generate new token (classic)"
3. Aktivieren Sie: `repo` (alle Unterrechte)
4. Generieren Sie den Token
5. Verwenden Sie diesen Token zum Pushen:

```bash
git push https://IHR_TOKEN@github.com/Elemirus1996/Einsatzueberwachung.git v1.7.1
```

## 🎯 Empfehlung

**Für jetzt (schnell):** Option 2 - Release über GitHub Web-Interface erstellen
- Geht am schnellsten
- Umgeht die Repository Rules
- Startet automatisch den Workflow

**Für die Zukunft:** Option 1 - Repository Rules anpassen
- Ermöglicht automatische Releases via Script
- Einmalige Konfiguration

## 📋 Nächste Schritte

1. **Lokalen Tag löschen:**
   ```bash
   git tag -d v1.7.1
   ```

2. **Einen der oben genannten Wege wählen**

3. **Workflow Status prüfen:**
   https://github.com/Elemirus1996/Einsatzueberwachung/actions

4. **Release downloaden:**
   https://github.com/Elemirus1996/Einsatzueberwachung/releases/tag/v1.7.1

## 🔗 Wichtige Links

- **Repository Rules:** https://github.com/Elemirus1996/Einsatzueberwachung/settings/rules
- **Releases:** https://github.com/Elemirus1996/Einsatzueberwachung/releases
- **Actions:** https://github.com/Elemirus1996/Einsatzueberwachung/actions
- **Settings:** https://github.com/Elemirus1996/Einsatzueberwachung/settings

## ❓ Support

Wenn Sie weitere Hilfe benötigen:
- Überprüfen Sie die GitHub Rules Dokumentation: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets
- Oder fragen Sie nach weiterer Unterstützung
