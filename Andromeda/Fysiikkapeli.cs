using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

/// @author Joni Laari
/// @version 18.12.2020
/// <summary>
/// Andromeda
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
 */


public class Andromeda : PhysicsGame
{
    private const double NOPEUS = 100;
    private const double HYPPYNOPEUS = 400;
    private PlatformCharacter pelaaja;
    private PlatformCharacter vihollinen;
    static readonly private Image menutausta = LoadImage("andromeda_menu_tausta");
    static readonly private Image galaksitausta = LoadImage("galaksi");
    static readonly private Image kauppatausta = LoadImage("kauppa_tausta");
    static readonly private Image vihusprite = LoadImage("vihu");
    static readonly private SoundEffect nappi = LoadSoundEffect("button");
    static readonly private SoundEffect kentanalku = LoadSoundEffect("kentta_alku");
    static private Image[] hahmonKavely = LoadImages("tile000", "tile001", "tile002", "tile003", "tile004", "tile005", "tile006", "tile007", "tile008", "tile009");
    private Label pistenaytto;
    private int kenttanro = 0;
    const int KENTTA_LKM = 3; //kenttien maksimimäärä
    private int kauppavierailu = 0; //käytetään ns. kauppabugin korjaamiseen, ettei galaksikartalle ilmesty kaksinkertaista määrää kohteita
    private int rahatilanne;
    private LaserGun laserase; //vihujen ase
    private PlasmaCannon plasmatykki; //bossin ase


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
    public void Alkuvalikko()
    {
        ClearAll();
        kenttanro = 0;
        rahatilanne = 0;

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


    //generoi galaksikartalle tietyn määrän kohdetähtiä ja kauppapaikan satunnaisiin sijainteihin
    public void LataaGalaksi()
    {
        LuoRahalaskuri();

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
                Alkuvalikko();
            }
        }
        //pomovastus
        /*
        else
        {

        }
        */
    }


    //luo punaisen täpän, joka indikoi pelattavaa kentää galaksikartalla
    public void KohdeIkoni(int kenttanumero)
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


    //luo galaksikarttaan keltaisen täpän kauppapaikkaa varten
    public void KauppaIkoni()
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


    //avaa kauppanäkymän, josta tarkoitus voida ostaa lisää HP:ta, ammuksia ja parempia aseita
    public void AvaaKauppa()
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


    //luo normaalin kentän
    public void LuoKentta()
    {
        kenttanro++;
        kauppavierailu = 0; //nollaa kauppavierailu-muuttujan, jotta kentän lopuksi tähdet luodaan uudelleen

        ClearAll();
        Gravity = new Vector(0, -800.0);

        nappi.Play();
        kentanalku.Play();
        MediaPlayer.Play("kenttatheme");
        MediaPlayer.IsRepeating = true;

        TileMap kentta = TileMap.FromLevelAsset(kenttanro.ToString());
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaRaha);
        kentta.SetTileMethod('N', LisaaPelaaja);
        kentta.SetTileMethod('P', LisaaPortti);
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
        LuoRahalaskuri();
    }


    //kenttäelementti
    void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.DarkBlue;
        taso.IgnoresPhysicsLogics = true;
        Add(taso);
    }


    //luo pelaajahahmon ja antaa sille aseen. TODO: aseen vaihtaminen
    void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {

        pelaaja = new PlatformCharacter(leveys, korkeus)
        {
            Position = paikka,
            Mass = 100.0,
            Animation = new Animation(hahmonKavely),
            Weapon = new LaserGun(10, 10),
            Size = new Vector(10, 10),
        };
        
        Add(pelaaja);
        AddCollisionHandler(pelaaja, "raha", TormaaRahaan);
        AddCollisionHandler(pelaaja, "portti", MenePorttiin);
    }


    //luo perusvihollisen. TODO: vihu ampuu myös takaisin
    void LisaaAlien(Vector paikka, double leveys, double korkeus)
    {
        vihollinen = new PlatformCharacter(leveys, korkeus)
        {
            Image = vihusprite,
            Position = paikka,
            Mass = 100.0,
            //Weapon = new LaserGun(30, 10),
        };
        Add(vihollinen);

        //AI
        PlatformWandererBrain vihuAivot = new PlatformWandererBrain();
        vihuAivot.JumpSpeed = 700;
        vihuAivot.TriesToJump = true;
        //Aivot käyttöön oliolle
        vihollinen.Brain = vihuAivot;
    }


    //lisää kenttään kerättävää rahaa
    void LisaaRaha(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject raha = PhysicsObject.CreateStaticObject((leveys * 0.5) , (korkeus * 0.5));

        raha.IgnoresCollisionResponse = true;
        raha.Shape = Shape.Circle;
        raha.Color = Color.Yellow;
        raha.Position = paikka;
        raha.Tag = "raha";

        Add(raha);
    }


    //Kentän poistumisportti, palauttaa pelaajan takaisin galaksikarttaan
    void LisaaPortti(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject portti = PhysicsObject.CreateStaticObject(leveys, korkeus);

        portti.IgnoresCollisionResponse = true;
        portti.Shape = Shape.Hexagon;
        portti.Color = Color.Red;
        portti.Position = paikka;
        portti.Tag = "portti";

        Add(portti);
    }


    //Näppäimet peliin
    void LisaaNappaimet()
    {
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


    //liikuttaa hahmoa
    void Liikuta(PlatformCharacter pelaaja, double NOPEUS)
    {
        pelaaja.Walk(NOPEUS);
        pelaaja.Animation.Start(1);
        pelaaja.Animation.FPS = 10;
    }


    //laittaa hahmon hyppäämään
    void Hyppaa(PlatformCharacter hahmo, double NOPEUS)
    {
        pelaaja.Jump(NOPEUS);
    }


    //rahan keräämisen käsittely
    void TormaaRahaan(PhysicsObject hahmo, PhysicsObject raha)
    {
        raha.Destroy();
        nappi.Play();
        rahatilanne += 100;
    }


    //porttiinmenon käsittely
    void MenePorttiin(PhysicsObject hahmo, PhysicsObject portti)
    {
        nappi.Play();
        ClearAll();
        LataaGalaksi();
    }


    //pelaajan aseella ampuminen
    void AmmuAseella(PlatformCharacter pelaaja)
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


    //vihollinen ampuu
    void VihuAmpuu(PlatformCharacter vihollinen)
    {
        PhysicsObject ammus = vihollinen.Weapon.Shoot();
        if (ammus != null)
        {
            ammus.Size *= 3;
            ammus.Color = Color.Red;
            ammus.Tag = "ammus";
        }
    }


    //käsittelee ampuma-aseiden osumat
    void AmmusOsui(PhysicsObject ammus, PhysicsObject kohde)
    {
        kohde.Destroy();
        ammus.Destroy();
    }


    //rahaa (pisteitä) seuraava laskuri
    public void LuoRahalaskuri()
    {
        IntMeter rahalaskuri = new IntMeter(rahatilanne);

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