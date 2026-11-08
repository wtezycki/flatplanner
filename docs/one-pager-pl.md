# Flatplanner: one-pager

Flatplanner pozwala kupującemu opisać własnymi słowami, czego szuka, i pokazuje, które mieszkania do niego pasują, które tylko częściowo, a w których ogłoszenie milczy na dany temat. Każdą ocenę popiera cytat z ogłoszenia albo konkretne miejsce w okolicy. Zakres: mieszkania na sprzedaż w Gdańsku, Gdyni i Sopocie, 197 ofert pobranych jednorazowo z otodom.pl. Źródło liczb: [ingest-report.md](ingest-report.md) i ewaluacje z 8 listopada 2026.

## Jakie dane i dlaczego

Wybrałem otodom, bo to tam są oferty sprzedaży z Trójmiasta, a pola w stałym formacie pozwalają zmierzyć, czego brakuje, zamiast zgadywać. Dzielnica, piętro, stan wykończenia i rok budowy są wypełnione w 92–100% ofert, więc filtry korzystają z nich wprost. Dane kontaktowe sprzedających usuwam już przy imporcie. Zanim zacząłem cokolwiek budować, przejrzałem oferty ręcznie i przy każdej zadawałem sobie pytanie: czego jeszcze musiałbym się dowiedzieć, zanim zadzwonię? Okazało się, że w polach brakuje tego, co przesądza o zakupie: czynsz podano w 40% ofert, windę w 66%, a okolicy nie opisuje żadne pole. Dlatego każda oferta dostaje **dossier**: krótkie podsumowanie, ryzyka dla kupującego poparte cytatami i miejsca w promieniu 1,5 km.

## Dane nieustrukturyzowane i słabej jakości

Każda z poniższych zasad wynika z ręcznego przeglądu ofert i opiera się na liczbach z raportu importu.

- **Brak informacji to nie „nie”.** W 68% ofert z rynku pierwotnego czynsz wynosi 0 zł, bo deweloper go nie podał. Zapisuję to jako „brak danych”, żeby kupujący nie zobaczył zerowego czynszu i nie zaniżył sobie miesięcznych kosztów.
- **Oznaczam, zamiast po cichu poprawiać.** 4 oferty z nietypową ceną zostają w bazie, ale są oznaczone. 26% ofert ma potencjalny duplikat, ale sprawdzone pary to zwykle różne mieszkania z tej samej inwestycji, więc niczego nie scalam. Ceną jest to, że jedna inwestycja może zająć kilka pierwszych miejsc w wynikach.
- **Przybliżona lokalizacja jest oznaczona.** 83 ze 197 ofert mają tylko przybliżoną lokalizację. Oferta o tym informuje, a najbliższy przystanek i park pokazuję tylko przy dokładnej lokalizacji.
- **Opis to deklaracja sprzedającego, a nie fakt.** Ryzyko trafia do dossier tylko wtedy, gdy opis faktycznie o nim mówi. 2 ze 137 ryzyk nie przeszły tej weryfikacji i zostały odrzucone.

## Gdzie i dlaczego AI

- **Przy imporcie: dossier.** Dla każdej oferty AI raz przygotowuje krótkie podsumowanie, listę ryzyk wynikających z opisu (dodatkowe koszty, stan prawny, hałas, dane niezgodne z treścią ogłoszenia) i wykaz miejsc w okolicy z OpenStreetMap. **Dlaczego:** pola nie odpowiedzą na pytanie „czy jest cicho?” ani „czy za parking trzeba dopłacić?”, a wyszukiwanie po słowach kluczowych zawodzi: słowo „cisza” pojawia się w 50 opisach, ale w 13 z nich chodzi wyłącznie o cichobieżne windy. **Efekt:** dossier ma każda ze 197 ofert, więc przy każdej widać, co jest w okolicy i jakie ryzyka wynikają z opisu, nawet jeśli sprzedający nie wypełnił żadnego pola. Przyjęto 135 ryzyk, 2 odrzucono jako niepoparte opisem.
- **W wyszukiwarce: czat.** Zamiast ustawiać filtry, kupujący pisze jedną wiadomość. Portal wyciąga z niej twarde kryteria (miasto, cenę, metraż) i potrzeby, które za nimi stoją („pracuję na nocki”, „mam psa”, „mama porusza się na wózku”), a potem ocenia każdą ofertę pod kątem każdej z tych potrzeb: pasuje, pasuje częściowo, nie pasuje albo ogłoszenie o tym nie mówi. Każda ocena ma uzasadnienie: cytat, miejsce z odległością albo pole z ogłoszenia. Jeśli uzasadnienia brakuje, ocena zmienia się na „ogłoszenie o tym nie mówi”. **Dlaczego:** kupujący myślą o swoich potrzebach, a nie o polach formularza, i żaden zestaw filtrów nie obejmie wszystkich życiowych sytuacji. **Efekt:** zamiast jednej listy dla wszystkich każdy dostaje oferty pogrupowane według tego, jak pasują do jego potrzeb, z krótkim uzasadnieniem przy każdej. W testach potrzeby zostały odczytane poprawnie w 15 z 15 zapytań, a 8 z 10 ocen zgodziło się z oczekiwanymi. Jedno zapytanie kosztuje mniej niż 0,01 USD, a wydatki mają limit. Gdy model jest niedostępny, wyszukiwanie po słowach kluczowych nadal działa.

## Kluczowe założenie

**Kupujący szybciej znajdują mieszkania, do których warto zadzwonić, i częściej kontaktują się z właściwymi sprzedającymi, gdy wyniki są dopasowane do ich sytuacji, a portal odpowiada na pytania, na które nie odpowiada ogłoszenie.** Lukę w danych zmierzyłem (patrz wyżej), zachowań kupujących już nie. Doszedłem do tego z perspektywy kupującego: sam szukam teraz mieszkania i najwięcej kłopotu sprawiają mi brakujące dane oraz to, że nie znam okolicy (czy jest cicho, co mam w zasięgu spaceru). To jednak doświadczenie jednej osoby, więc jest źródłem hipotezy, a nie dowodem. Hipotezą jest też związek z biznesem: kupujący, który nie wie, czy mieszkanie mu pasuje, nie dzwoni; sprzedający rzadko uzupełniają ogłoszenia; a ogłoszeniodawcy płacą właśnie za lepiej dopasowane leady. Założenie się nie sprawdzi, jeśli na podstawie samych ogłoszeń i filtrów kupujący decydują równie szybko i równie trafnie. Dokładnie to sprawdza opisany niżej test A/B.

## Metryka sukcesu

**Odsetek wyszukiwań zakończonych kliknięciem „Zobacz na otodom” przy jednej z pięciu pierwszych ofert; w teście A/B czat porównany z listą z filtrami.** W prototypie to kliknięcie jest najbliższym odpowiednikiem kontaktu ze sprzedającym; prawdziwy portal liczyłby same kontakty. Dodatkowo obserwuję czas do pierwszego kliknięcia (czy kupujący trafia szybciej), a żeby wzrost nie odbył się kosztem czegoś innego, także odsetek odpowiedzi „ogłoszenie o tym nie mówi”, wyszukiwania bez wyników i koszt jednego wyszukiwania. Prototyp nie zbiera jeszcze zdarzeń, więc to plan testu, a nie jego wynik.

## Scenariusz porażki

**Błędne „pasuje” podane z pełnym przekonaniem.** Kod sprawdza, czy cytowane zdanie rzeczywiście jest w ogłoszeniu, ale nie to, czy model dobrze je zrozumiał. Do tego 83 ze 197 lokalizacji są przybliżone, więc odległość podana w ocenie może się nie zgadzać. Kupujący, który usłyszy, że mieszkanie nadaje się dla osoby na wózku, pojedzie na oglądanie na próżno i przestanie ufać wszystkim ocenom. Uzasadnienie przy każdej ocenie pozwala sprawdzić ją przed telefonem, ale ostrożność ma swoją cenę: w 94 z 216 ocen (44%) system odpowiada, że ogłoszenie o tym nie mówi. Wynik 8/10 opiera się na trzech ofertach, które przeczytałem sam, więc to wstępny test, a nie pomiar. Jak często ocena „pasuje” jest błędna, jeszcze nie zmierzyłem; to jest na liście poniżej.

## Gdybym miał więcej czasu (od najważniejszego)

1. **Śledzenie zdarzeń i test A/B na prawdziwym ruchu:** bez tego nie wiadomo, czy rozwiązanie podnosi wartość leada.
2. **Porównanie ocen z odpowiedziami sprawdzonymi ręcznie, żeby wiedzieć, jak często „pasuje” jest błędne:** zaufanie do ocen to podstawa produktu.
3. **Dossier tworzone na bieżąco dla nowych ofert, ze znanym kosztem na ofertę:** w skali portalu dane muszą być aktualne.
4. **Pokazywanie sprzedającym, na które pytania kupujących ich ogłoszenie nie odpowiada:** kupujący dostają mniej odpowiedzi „ogłoszenie o tym nie mówi”, a jakość ogłoszenia staje się czymś, co portal może sprzedać.

**Nie tylko mieszkania.** To podejście nie jest związane z nieruchomościami: większość wyszukiwarek ma więcej informacji w opisach i opiniach niż w polach, więc potrzebę opisaną własnymi słowami można porównać z każdym wynikiem, podając dowód albo uczciwe „nie wiadomo”. Na przykład w ZnanyLekarz pacjent mógłby szukać lekarza, który jest spokojny przy dziecku bojącym się wizyt, albo gabinetu bez schodów, i przy każdym wyniku dostać cytat z profilu lub opinii.
