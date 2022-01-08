# FlowProtocol

*FlowProtocol* ist eine Anwendung, mit der man Check-, bzw. Prüflisten zu ausgewählten Themengebieten anhand einfacher Kontrollfragen erstellen kann. Mithilfe der Kontrollfragen wird dabei festgestellt, welche Aspekte relevant sind und welche nicht und welche Prüfpunkte dementsprechend am Ende in die Liste aufgenommen werden. Das Ziel dieser Vorgehensweise ist die Sicherstellung von Qualität durch die Bereitstellung möglichst effizienter Prüflisten, die so gut wie möglich auf die tatsächlich relevanten Punkte beschränkt bleiben.

## Anwendergruppen
Diese Vorgehensweise ist vielseitig einsetzbar und daher auch für verschiedene Anwendergruppen innerhalb einer Einrichtung verwendbar. Im Umfeld der Softwareentwicklung könnte dies zum Beispiel die Anwendergruppen „Entwicklung“, „Product Owner“ und „UX und Design“ sein. Entsprechend ist es sinnvoll, zunächst die verschiedenen Anwendergruppen auf unterster Ebene in der Konfiguration von *FlowProtocol* abzubilden. Dies geht ganz einfach dadurch, dass man im Basisverzeichnis für die Vorlagen (siehe appsettings.json) Unterverzeichnisse für jede Anwendergruppe mit einer sprechenden Benennung anlegt. Die jeweilige Anwendergruppe kann dann unter dem Menü »Anwendergruppen« aufgerufen werden.

## Vorlagen
Die Vorlagen sind die Kernelemente von FlowProtocol. Sie beinhalten die Fragen samt Antwortmöglichkeiten und die davon abhängigen Prüfpunkte. Jede Vorlage sollte sich auf ein spezielles Thema oder eine bestimmte Art von Aufgabenstellung beziehen, um möglichst direkt mit möglichst spezifischen Fragen starten zu können. Ein solches dem Gebiet kann zum Beispiel eine bestimmte Art von Entwicklung sein, die nach einem vorgegebenen Muster abläuft und darin überschaubare Varianten aufweisen kann. Diese lassen sich dann mithilfe von passenden Fragen eingrenzen. In gleicher Weise kann man auch fachliche Themen in Form einer Vorlage bereitstellen, indem man versucht, die komplette fachliche Spezifikation zu erfassen, und deren potentielle Beeinflussung durch eine Maßnahme durch geeignete Fragen festzustellen. Vorlagen können wiederum in Vorlagengruppen gruppiert werden, etwa wie in den gerade genannten Beispielen in „Musterentwicklungen“ und „Fachliche Themen“, eventuell zusätzlich unterteilt durch Produkt-, und Komponentennamen. Die Unterteilung erfolgt in gleicher Weise wie bei den Anwendergruppen durch entsprechend benannte Unterverzeichnisse unterhalb der Anwendergruppen-Verzeichnisse. Eine mehrstufige Hierarchie ist möglich, wird jedoch in der Darstellung ignoriert.


## Aufbau einer Vorlage
Vorlagen sind einfache Textdateien mit der Dateiendung „*.qfp“ (Quick Flow Protocol). Die Syntax besteht aus ganz wenigen Steuerzeichen und wurde speziell darauf ausgelegt, dass Vorlagen sehr einfach mit einem beliebigen Editor erstellt und bearbeitet werden können. Der Inhalt einer Vorlage besteht im Wesentlichen aus den Komponenten Kommentar (// und ///), Frage (?), Antwort (#), Prüfpunkt (>>) und Einbindungen (++), die in beliebiger Tiefe verschachtelt werden können. Fragen und Antworten haben jeweils eine möglichst kurze Kennung, mit denen der Verlauf der Durchführung als URL-Parameter abgebildet wird. Zeilenumbrüche und Einrückungen sind essenziell zur Abbildung der Struktur, wobei die Anzahl der Zeichen relevant ist. Das nachfolgende Beispiel zeigt, wie die Syntax aufgebaut ist:
```
/// Kontrollpunkte für die Erstellung von Listenbereichen.
/// Version 1.0 (c) W. Maier
?F1: Welche Art von Liste wurde erstellt?
    #E: Editierbare Liste
        >> Elemente in der Liste können bearbeitet werden.
        ?E1: Können im Umfeld der Liste neue Elemente erstellt werden?
            #J: Ja
                >> Neu erstellte Elemente werden direkt in der Liste angezeigt.
                ~Include ListeErstellung: $Bereich=Test; $Postfix=1
            #N: Nein
    #A: Auswahlliste
        >> Elemente in der Liste können nicht bearbeitet werden.
        >> Die Auswahl einzelner Elemente ist möglich und wird korrekt verarbeitet. 
```

## Einbindungen
Manche Abschnitte aus Vorlagen sind eventuell so allgemein, dass man sie in gleichger Form in mehreren Vorlagen verwenden möchte. In diesem Fall kann man diese Abschnitte als Funktionsdatei mit der Dateiendung „*.qff“ (Quick Flow Protocol) in eine eigene Datei auslagern und über eine Einbindung (~Include Dateiname) an einer bestimmten Stelle in einer Vorlage aufrufen. Die Datei muss dabei im gleichen Verzeichnis liegen. Bei einem solchen Aufruf lassen sich auch Variablen mit Werte belegen, z.B. $Bereich=Test; $Postfix=1, so dass beim Einlsen der Datei Ersetzungen möglich sind, z.B.
```
?T$Postfix: Besitzt der Bereich $Bereich einen Berichtskontext?
```
zu
```
?T1: Besitzt der Bereich Test einen Berichtskontext?
```
wodurch auch mehfache Aufrufe innerhalb derselben Vorlage möglich sind.