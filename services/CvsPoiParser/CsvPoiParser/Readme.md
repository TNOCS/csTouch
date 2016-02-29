# Handleiding

ik heb een tooltje gemaakt om een CSV file in te lezen, en te vertalen naar een DataService (*.ds) file zdd deze als layer gebruikt kan worden in ons Common Sense framework.

Na het openen van een CSV (inclusief HEADERs en iets van een adres) wordt de CSV automatisch geparsed, en zo goed mogelijk worden de beschikbare headers vertaald naar iets zinvols, bv adres → adres label, X → X coordinate voor Rijksdriehoek, lat → latitude etc.. Dit is eventueel zelf aan te passen met de Action kolom. Daarnaast kun je aangegeven of je een Popup wil, en zo ja, de index positie, het type, etc.

Ook handig is dat je een specifieke kolom als TypeName kunt gebruiken: dwz dat hij deze waarde gebruikt om een poitype te genereren.

Tenslotte kun je de naam en description nog laten genereren en hierbij cellen combineren. Een beperking is nu nog dat de description niet automatisch toegevoegd wordt in de popup.

Na het drukken van start wordt de DS aangemaakt: hierbij wordt ook een lat/lon positie gegenereerd door of gebruik te maken van lat/long of X/Y RD in de database, of postcode+huisnummer op te zoeken in de BAG (indien aanwezig: connectionstring is in te stellen in de *.exe.config), of Google aan te roepen. Dit laatste, wegens API beperkingen, gaat langzaam (=1 query per seconde).