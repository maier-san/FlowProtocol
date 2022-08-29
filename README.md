# *FlowProtocol*

## Grundidee
*FlowProtocol* ist eine Anwendung, mit der man Aufgaben-, bzw. Prüflisten zu ausgewählten Themengebieten anhand einfacher Kontrollfragen erstellen kann. Der Name leitet sich ab aus Flow für „Flussdiagramm“ und „Protokoll“ und steht für das Grundkonzept der Anwendung, anhand eines verzweigten Entscheidungsbaums ein Protokoll zusammenzustellen. Die Anwendung funktioniert dabei wie folgt:  Nach Auswahl einer Vorlage wird eine Reihe von Multiple-Choice-Fragen gestellt, mit denen die relevanten Aspekte eines Themas Schritt für Schritt eingegrenzt werden, und aus denen am Ende eine genau abgestimmte Auflistung von Punkten zusammengestellt wird, die beispielsweise als Protokoll verwendet werden können.

Die Grundidee der Anwendung besteht darin, ein Medium zu bieten, um Spezialwissen ohne großen Aufwand zu erfassen, um es in sehr einfacher Form für andere bereitzustellen, ohne dass es dafür vermittelt und erworben werden muss. Die Erstellung der Vorlagen kann und soll direkt durch die Menschen erfolgen, die über das jeweilige Wissen verfügen, und von einer breiten Menge an Nutzern angewendet werden.

## Anwendungsbereiche
Die Anwendungsbereiche für diese Anwendung sind sehr vielfältig. Die ursprüngliche
Intention war die Aufstellung von Prüflisten für wiederkehrende komplexe Tätigkeiten,
die in vielen einzelnen Aspekten variieren können, sodass jeweils nur eine bestimmte
Auswahl von Prüfpunkten relevant ist. Eine Gesamtprüfliste, bei der immer ein
Großteil der Punkte hätte ignoriert werden müssen, wäre sowohl aufwendig, als
auch fehlerträchtig, und daher nicht effektiv. *FlowProtocol* kann hier durch sein Fragekonzept
die für jeden Einzelfall relevanten Aspekte herausfiltern, und auf diese
Weise sogar beliebig stark ins Detail gehen. Gleichzeitig hilft dem Anwender die
Beantwortung der Fragen auch, sich gedanklich ausführlich mit seiner Aufgabe auseinanderzusetzen.
Durch die Vermeidung offener Fragen wird gleichzeitig das Risiko
minimiert, dass Dinge vergessen werden. Das Ergebnis ist ein zu 100% relevantes
und vollständiges Protokoll.

Ein besonders positiver Nebeneffekt dieser Vorgehensweise besteht darin, dass durch
einfaches Auswählen von Antworten am Ende ein Ergebnisdokument entsteht, das
nicht mehr manuell erstellt werden muss. Das Anwendungsgebiet dehnt sich damit
auf alle Arten von Dokumenten aus, die sich aus einer Aufzählung von Textbausteinen
zusammensetzen, deren Zusammenstellung über eine Reihe iterierter Fragen
erfolgen kann. Dies umfasst beispielsweise Bestandsaufnahmen, Analyseprotokolle,
Zusammenfassungen, Aufgabenlisten und vieles andere mehr. Für kleinere Systeme
lassen sich sogar mit überschaubarem Aufwand Vorlagen erstellen, die nicht nur
das Ergebnis einer Analyse zusammenfassen, sondern ergänzend dazu auch schon
Lösungsansätze und mögliche weitere Schritte beschreiben. Man kann so vom Prinzip her ein
kleines Expertensystem erstellen, das wertvolles Spezialwissen eines Experten in sehr
einfacher Form für andere Mitarbeiter verfügbar macht, ähnlich wie ein Chatbot.
Das schont wertvolle Unternehmensressourcen und beugt Verfügbarkeitsengpässen
vor.

Eine besonders nützliche Anwendung ist die Erstellung von interaktiven Anleitungen.
Hierbei werden ebenfalls Rahmenbedigungen und einzelne Textbausteine im
Vorfeld abgefragt, und in Abhängigkeit davon die für den vorliegenden Fall relevanten
Schritte der Anleitung ausgegeben. Die Möglichkeit, kontextbezogene und
durch Textersetzungen angepasste Codepassagen in die Ausgabe einzufügen, macht
*FlowProtocol* zu einem besonders nützlichen Werkzeug in der Softwareentwicklung,
wo derartige wiederkehrende Muster zum Tagesgeschäft gehören. Die Kombination
von Anleitung und Codegenerator erhöht nicht nur die Effizient, sondern auch die
Qualität.

Auch kleinere personalisierte Umfragen lassen sich über Vorlagen realisieren, wenn
man die Teilnehmer zur Rücksendung der Ergebnisse instruiert. Es gibt sogar einen
speziellen Befehl, der bei der Rangfolgenbestimmung von kleineren Listen unterstützt,
indem die darin vorkommenden Elemente paarweise gegenübergestellt werden. 
Für wichtige Rangfolgen wie beispielsweise Projektprioritäten ist es
natürlich besser, man hat detaillierte und bewährte Kriterien, die sich objektiv auf
alle Elemente der Liste anwenden lassen, und die am Ende über einen Zahlenwert
zu einer Rangfolge oder Auswahl führen. Auch solche Bewertungen lassen sich mit
*FlowProtocol* sehr leicht umsetzen und für alle verfügbar machen.

## Konfiguration
*FlowProtocol* ist bewusst einfach gehalten und soll es auch bleiben. Als Web-Anwendung kann der Dienst innerhalb einer Einrichtung zentral zur Verfügung gestellt werden. Die einzige notwendige Konfiguration ist die Angabe eines serverseitig verfügbaren Vorlagenordners, in dem sich die Vorlagen befinden (einstellbar über die Eigenschaft TemplatePath in der Datei appsettings.json). Es werden weder Datenbank noch zusätzliche Dienste benötigt. Die Vorlage-Dateien sind normale Textdateien (in UTF-8-Codierung), die die eigentliche Logik enthalten, und die über die Anwendung ausgeführt werden. Die Syntax der Vorlage-Dateien ist ebenfalls sehr einfach und bewusst für die manuelle Erstellung in einem Editor vorgesehen. Der Aufbau ist zeilenbasiert und verwendet Einrückung zur Abbildung der Verschachtelung. Die Erstellung einer ersten eigenen Vorlage ist innerhalb von 2 Minuten möglich. Eine ausführliche Beschreibung des Sprachumfangs wird im Anwenderhandbuch (https://github.com/maier-san/FlowProtocol/blob/main/Doc/FlowProtocol.pdf) gegeben. Das Ergebnis einer Bearbeitung wird am Ende auf dem Bildschirm ausgegeben, von wo aus es in die Zwischenablage übernommen werden kann. Tatsächlich wird in den meisten Fällen eine Weiterverarbeitung in einem anderen System (E-Mail, CRM, Projektverwaltung) erfolgen, wo noch Begriffe ergänzt und Prozesse angestoßen werden können und eine revisionssichere Verwaltung möglich ist.

Die Organisation der Vorlagen erfolgt auf zwei Ebenen. Auf unterster Ebene ist eine Unterteilung in Anwendergruppen vorgesehen, die jeweils ihre eigenen Vorlagen verwenden. In einem Unternehmen oder einer Einrichtung wird sich diese Aufteilung normalerweise am Mitarbeiterorganigramm orientieren. Ein Beispiel wäre die Aufteilung in Vertrieb, Marketing, Entwicklung, Administration. Jeder Ordner im Vorlagenordner wird in *FlowProtocol* als Anwendergruppe auf der Seite Anwendergruppen angezeigt. Innerhalb dieser Ordner kann man die Vorlagen entweder direkt ablegen oder in weitere Unterordner unterteilen, um fachliche Gruppen zu bilden. Der Pfad einer Vorlage innerhalb dieser Struktur findet sich auch in der URL wieder, die innerhalb der Anwendung erzeugt wird, und kann so auch direkt als Link in einer E-Mail, einem Vorgang, im Wiki oder an einer anderen Stelle im Intranet bereitgestellt werden.

## Technische Ergänzungen
*FlowProtocol* arbeitet vollständig zustandslos in dem Sinne, dass bei der Benutzung keinerlei Daten durch die Anwendung gespeichert werden. Es gibt weder eine Datenbank, noch eine Benutzerverwaltung und der Zugriff auf das Dateisystem erfolgt nur lesend. Die Verwaltung der gegebenen Antworten erfolgt vollständig in der URL, was die Möglichkeit bietet, zurückzuspringen oder einen Zwischenstand als Lesezeichen zu speichern oder zu versenden, allerdings werden Aufrufe von URLs in Unternehmen teilweise durch die IT-Infrastruktur protokolliert, sodass eine Verarbeitung schützenswerter Daten auf jeden Fall dahingehend betrachtet werden sollte.

*FlowProtocol* steht unter der MIT Lizenz und ist unter https://github.com/maier-san/FlowProtocol frei verfügbar.

Viel Freude beim Erstellen von Vorlagen und deren Anwendung!

Copyright © 2022 Wolfgang Maier
