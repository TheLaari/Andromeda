using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;

/// @author Joni Laari
/// @version 26.1.2021
/// <summary>
/// Andromedan galaksi on vaarallinen paikka ja palkkasoturille riittää töitä. Pelaaja on eräs tällainen galaktinen tuholaistorjuja, joka rahaa vastaan hävittää inhoja otuksia.
/// </summary>

/*
 TODO:
- Viholliset ampumaan
- Lisäkenttien tekeminen
- HP
- ammukset
- aseen vaihtaminen
- kaupasta ostaminen
- kun x kenttää suoritettu, aukeaa loppubossi
- loppubossi
- kaupan tähtibugi kuntoon
- tähdet ja kauppa samaan aliohjelmaan
- galaksikarttojen täppiin himmentymisanimaatio (bonuksen bonus jos jaksan)
- tietoja seuraavasta kentästä ilmestyy, kun hiiren vie karttamerkin päälle (jälleen bonuksen bonus)
- 15.12 Liikkuminen toimimaan, kentän layout kuntoon
- valikoiden luonnin yhdistäminen


ht3
-dokumentaatio
-kaikki privaatiksi
-aliohjelmien yhdistäminen (taso, portti, raha)
 */


public class Andromeda : PhysicsGame
{
    private PlatformCharacter pelaaja;
    private PlatformCharacter vihollinen;
    
    static readonly private Image menutausta = LoadImage("andromeda_menu_tausta");
    static readonly private Image galaksitausta = LoadImage("galaksi");
    static readonly private Image kauppatausta = LoadImage("kauppa_tausta");
    static readonly private Image vihusprite = LoadImage("vihu");
    static readonly private SoundEffect nappi = LoadSoundEffect("button");
    static readonly private SoundEffect kentanAlku = LoadSoundEffect("kentta_alku");
    private static readonly Image[] hahmonKavely = LoadImages("tile000", "tile001", "tile002", "tile003", "tile004", "tile005", "tile006", "tile007", "tile008", "tile009");
    
    private Label pistenaytto;
    private readonly IntMeter rahalaskuri = new IntMeter(0);

    private int kenttanro = 0;

    private int kauppavierailu = 0; //käytetään ns. kauppabugin korjaamiseen, ettei galaksikartalle ilmesty uutta satsia kohteita aina kentän 
    //private int rahatilanne;

    private readonly LaserGun laserase; //vihujen ase TBI
    private readonly PlasmaCannon plasmatykki; //bossin ase TBI


    /// <summary>
    /// Pelin käynnistävä aliohjelma
    /// </summary>
    public override void Begin()
    {
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        //Ladataan pelin alkuvalikko
        Alkuvalikko();
    }


    /// <summary>
    /// Päävalikko, josta peli alkaa
    /// </summary>
    private void Alkuvalikko()
    {
        ClearAll();
        kenttanro = 0;
        rahalaskuri.Value = 0;

        MultiSelectWindow paaValikko = new MultiSelectWindow("WELCOME ABOARD, COMMANDER", "Explore the Galaxy", "Disembark");
        MediaPlayer.Play("menutheme");
        MediaPlayer.IsRepeating = true;

        Level.Background.Image = menutausta;
        paaValikko.Color = Color.AshGray;
        Add(paaValikko);

        paaValikko.AddItemHandler(0, LataaGalaksi);
        //alkuvalikko.AddItemHandler(1, topLista);
        paaValikko.AddItemHandler(1, Exit); //vaihdettava 2:ksi kun toplista toimii
        IsPaused = true;
    }


    /*
     //alunperin tarkoitus oli lisätä myös lentelykenttiä, jota varten tässä valittaisiin alus.
    public void Alusvalikko()
    {
        nappi.Play();
        MultiSelectWindow alusvalikko = new MultiSelectWindow("Pick your spacecraft", "Cotentin", "Caledonia", "Calabria");
        Level.Background.Image = menutausta;
        alusvalikko.Color = Color.AshGray;
        Add(alusvalikko);
        alusvalikko.AddItemHandler(0, LataaGalaksi);
        alusvalikko.AddItemHandler(1, LataaGalaksi);
        alusvalikko.AddItemHandler(2, LataaGalaksi);
        IsPaused = true;
    }
    */


    /// <summary>
    /// Generoi galaksikartalle tietyn määrän kohdetähtiä ja kauppapaikan satunnaisiin sijainteihin
    /// </summary>
    private void LataaGalaksi()
    {
        const int KENTTA_LKM = 3; //kenttien maksimimäärä
        LuoPistenaytto();

        Camera.Reset();
        Camera.Zoom(1);
        Camera.StayInLevel = false;

        nappi.Play();

        IsPaused = false;
        Level.Background.Image = galaksitausta;

        if (kenttanro < KENTTA_LKM && kauppavierailu == 0)
        {
            MediaPlayer.Play("Uncharted");
            MediaPlayer.IsRepeating = true;

            //"alustetaan" tähtilista
            List<int> tahdet = new List<int>();
            for (int i = 0; i < KENTTA_LKM; i++)
            {
                tahdet.Add(i);
            }

            //tavoitteena on että joka "kierroksella" on kolme valittavaa kenttää, joista jokainen on erilainen. 
            //Siispä kolmella kentällä ennen loppupomoa olisi mahdollista pelata 12! / (3! * (12-3)!) = 220 eri kenttäyhdistelmää
            for (int j = 0; j < KENTTA_LKM; j++)
            {
                KohdeIkoni(tahdet[j]);
            }
            KauppaIkoni();
        }
        else
        {
            if (kenttanro == KENTTA_LKM)
            {
                Alkuvalikko();//tulee vaihtaa bossikentäksi kun se on tehty
            }
        }
        //pomovastus TBI
        /*
        else
        {

        }
        */
    }


    /// <summary>
    /// Luo galaksikarttaan punaisen napin kenttää varten.
    /// </summary>
    /// <param name="kenttanumero">käytetään kentän uniikin tägin luomista varten</param>
    private void KohdeIkoni(int kenttanumero)
    {
        GameObject system = new GameObject(15, 15)
        {
            Shape = Shape.Rectangle,
            Color = Color.Red
        };
        Vector tahdenPaikka = Level.GetRandomPosition();
        system.Position = tahdenPaikka;

        system.Tag = ("system_" + kenttanumero);

        Add(system);
        Mouse.ListenOn(system, MouseButton.Left, ButtonState.Down, LuoKentta, "Explore the chosen system");
    }


    /// <summary>
    /// Luo galaksikarttaan keltaisen napin kauppapaikkaa varten.
    /// </summary>
    private void KauppaIkoni()
    {
        Widget kauppa = new Widget(15, 15)
        {
            Shape = Shape.Rectangle,
            Color = Color.Yellow
        };
        Vector tahdenPaikka = Level.GetRandomPosition();
        kauppa.Position = tahdenPaikka;

        kauppa.Tag = "kauppa";

        Add(kauppa);
        Mouse.ListenOn(kauppa, MouseButton.Left, ButtonState.Down, AvaaKauppa, "Visit a trade port");
    }


    /// <summary>
    /// Avaa kauppanäkymän, josta tarkoitus voida ostaa lisää HP:ta, ammuksia ja parempia aseita.
    /// </summary>
    private void AvaaKauppa()
    {
        //ClearAll();
        nappi.Play();
        kauppavierailu++;

        MultiSelectWindow kauppavalikko = new MultiSelectWindow("Welcome, Commander! Feel free to browse!", " Buy a Heal-Pak", "Buy Ammunition", "Buy a Super Shotgun", "Leave Port");
        Level.Background.Image = kauppatausta;
        kauppavalikko.Color = Color.AshGray;
        Add(kauppavalikko);

        kauppavalikko.AddItemHandler(0, LataaGalaksi); //lääkepaketti
        kauppavalikko.AddItemHandler(1, LataaGalaksi); //ammuksia
        kauppavalikko.AddItemHandler(2, LataaGalaksi); //parempi ase
        kauppavalikko.AddItemHandler(3, LataaGalaksi); //poistu (kunnossa!)
        IsPaused = true;
    }


    /// <summary>
    /// Luo normaalin kentän.
    /// </summary>
    private void LuoKentta()
    {
        kenttanro++;
        kauppavierailu = 0; //nollaa kauppavierailu-muuttujan, jotta kentän lopuksi tähdet luodaan uudelleen

        ClearAll();
        Gravity = new Vector(0, -800.0);

        nappi.Play();
        kentanAlku.Play();
        MediaPlayer.Play("kenttatheme");
        MediaPlayer.IsRepeating = true;

        //luo kenttänumeron mukaisen kentän tiedostosta
        TileMap kentta = TileMap.FromLevelAsset(kenttanro.ToString());
        kentta.SetTileMethod('1', LisaaObjekti, Color.DarkBlue, "taso", Shape.Rectangle); //sininen kenttäelementti ykköstasoon
        kentta.SetTileMethod('2', LisaaObjekti, Color.MediumPurple, "taso", Shape.Rectangle); //purppura kenttäelementti kakkostasoon
        kentta.SetTileMethod('3', LisaaObjekti, Color.BrightGreen, "taso", Shape.Rectangle); //vihreä kenttäelementti
        kentta.SetTileMethod('P', LisaaObjekti, Color.Red, "portti", Shape.Octagon); //kentän poistumisportti
        kentta.SetTileMethod('*', LisaaRaha);
        kentta.SetTileMethod('N', LisaaPelaaja);
        kentta.SetTileMethod('V', LisaaAlien);
        kentta.Execute(10, 10);

        Level.CreateBorders();
        Level.Background.Color = Color.Black;
        LisaaNappaimet();

        //Kamera
        Camera.ZoomFactor = 2;
        Camera.Follow(pelaaja);
        //Camera.StayInLevel = true;

        //rahalaskuri
        LuoPistenaytto();
    }


    /// <summary>
    /// Lisää kenttäobjekti, kuten seinä, raha tai poistumisportti.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    /// <param name="vari"></param>
    /// <param name="tag"></param>
    /// <param name="muoto"></param>
    private void LisaaObjekti(Vector paikka, double leveys, double korkeus, Color vari, string tag, Shape muoto)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);

        taso.Position = paikka;
        taso.Color = vari;
        taso.IgnoresPhysicsLogics = true;
        taso.Tag = tag;
        taso.Shape = muoto;
        

        Add(taso);
    }


    /// <summary>
    /// Luo pelaajahahmon ja antaa sille aseen. TODO: aseen vaihtaminen
    /// </summary>
    /// <param name="paikka">pelaajalle annettava sijainti</param>
    /// <param name="leveys">pelaajan leveys</param>
    /// <param name="korkeus">pelaajan korkeus</param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja = new PlatformCharacter(leveys, korkeus)
        {
            Position = paikka,
            Mass = 100.0,
            Animation = new Animation(hahmonKavely),
            Weapon = new LaserGun(10, 10),
            Size = new Vector(10, 15),
        };
        
        Add(pelaaja);
        AddCollisionHandler(pelaaja, "raha", TormaaRahaan);
        AddCollisionHandler(pelaaja, "portti", MenePorttiin);
    }


    /// <summary>
    /// Luo perusvihollisen. TODO: vihu ampuu myös takaisin
    /// </summary>
    /// <param name="paikka">vihun sijainti</param>
    /// <param name="leveys">vihun leveys</param>
    /// <param name="korkeus">vihun korkeus</param>
    private void LisaaAlien(Vector paikka, double leveys, double korkeus)
    {
        vihollinen = new PlatformCharacter(leveys, korkeus)
        {
            Image = vihusprite,
            Position = paikka,
            Mass = 100.0,
            //Weapon = new LaserGun(30, 10), //vihun ase
        };
        Add(vihollinen);

        //AI
        PlatformWandererBrain vihuAivot = new PlatformWandererBrain
        {
            JumpSpeed = 700,
            TriesToJump = true
        };
        //Aivot käyttöön oliolle
        vihollinen.Brain = vihuAivot;
        vihollinen.Tag = "vihu";
    }


    /// <summary>
    /// lisää kenttään kerättävä kolikko
    /// </summary>
    /// <param name="paikka">kolikon sijainti</param>
    /// <param name="leveys">kolikon leveys</param>
    /// <param name="korkeus">kolikon korkeus</param>
    private void LisaaRaha(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject raha = PhysicsObject.CreateStaticObject((leveys * 0.5) , (korkeus * 0.5));

        raha.IgnoresCollisionResponse = true;
        raha.Shape = Shape.Circle;
        raha.Color = Color.Yellow;
        raha.Position = paikka;
        raha.Tag = "raha";

        Add(raha);
    }


    /// <summary>
    /// Näppäimet peliin
    /// </summary>
    private void LisaaNappaimet()
    {
        const double NOPEUS = 100;
        const double HYPPYNOPEUS = 400;

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu oikealle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Space, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, HYPPYNOPEUS);
        Keyboard.Listen(Key.F, ButtonState.Pressed, AmmuAseella, "Ammu", pelaaja);

        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Exit, "Poistu pelistä");

        ControllerOne.Listen(Button.DPadLeft, ButtonState.Down, Liikuta, "Pelaaja liikkuu vasemmalle", pelaaja, -NOPEUS);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Down, Liikuta, "Pelaaja liikkuu oikealle", pelaaja, NOPEUS);
        ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, HYPPYNOPEUS);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// liikuttaa hahmoa
    /// </summary>
    /// <param name="pelaaja">pelaajahahmo</param>
    /// <param name="nopeus">pelaajan liikkumisnopeus</param>
    private void Liikuta(PlatformCharacter pelaaja, double nopeus)
    {
        pelaaja.Walk(nopeus);
        pelaaja.Animation.Start(1);
        pelaaja.Animation.FPS = 10;
    }


    /// <summary>
    /// laittaa hahmon hyppäämään
    /// </summary>
    /// <param name="hahmo">hyppäävä pelaajahahmo</param>
    /// <param name="hyppynopeus">hyppynopeus</param>
    private void Hyppaa(PlatformCharacter hahmo, double hyppynopeus)
    {
        pelaaja.Jump(hyppynopeus);
    }


    /// <summary>
    /// rahan keräämisen käsittely
    /// </summary>
    /// <param name="hahmo">rahaan törmäävä pelihahmo</param>
    /// <param name="raha">kolikko, johon törmätään</param>
    private void TormaaRahaan(PhysicsObject hahmo, PhysicsObject raha)
    {
        raha.Destroy();
        nappi.Play();
        rahalaskuri.Value += 100;
    }


    /// <summary>
    /// Porttiinmenon käsittely
    /// </summary>
    /// <param name="hahmo">porttiin osuva (pelaaja)hahmo</param>
    /// <param name="portti">portti johon törmätään</param>
    private void MenePorttiin(PhysicsObject hahmo, PhysicsObject portti)
    {
        nappi.Play();
        ClearAll();
        LataaGalaksi();
    }


    /// <summary>
    /// Pelaajan aseella ampuminen
    /// </summary>
    /// <param name="pelaaja">ampuva pelaajahahmo</param>
    private void AmmuAseella(PlatformCharacter pelaaja)
    {
        PhysicsObject ammus = pelaaja.Weapon.Shoot();
        if (ammus != null)
        {
            ammus.Size *= 3;
            ammus.Color = Color.Red;
            ammus.Tag = "ammus";
        }
        pelaaja.Weapon.ProjectileCollision = AmmusOsui;
    }


    /// <summary>
    /// Vihollinen ampuu
    /// </summary>
    /// <param name="vihollinen">ampuva vihollinen</param>
    private void VihuAmpuu(PlatformCharacter vihollinen)
    {
        PhysicsObject ammus = vihollinen.Weapon.Shoot();
        if (ammus != null)
        {
            ammus.Size *= 3;
            ammus.Color = Color.Red;
            ammus.Tag = "ammus";
        }
    }


    /// <summary>
    /// käsittelee ampuma-aseiden osumat
    /// </summary>
    /// <param name="ammus">osuva ammus</param>
    /// <param name="kohde">kohde johon ammus osuu</param>
    private void AmmusOsui(PhysicsObject ammus, PhysicsObject kohde)
    {
        if (kohde.Tag.ToString() == "vihu")
        {
            kohde.Destroy();
            ammus.Destroy();
            rahalaskuri.Value += 100;
        }
        else
        {
            if (kohde.Tag.ToString() == "seinä")
            {
                ammus.Destroy();
            }
        }
    }


    /// <summary>
    /// rahaa (pisteitä) seuraava laskuri
    /// </summary>
    private void LuoPistenaytto()
    {
        pistenaytto = new Label
        {
            X = Screen.Left + 100,
            Y = Screen.Top - 100,
            TextColor = Color.White,
            Color = Color.Black
        };
        pistenaytto.BindTo(rahalaskuri);
        pistenaytto.Title = ("Credits");

        Add(pistenaytto);
    }
    

}