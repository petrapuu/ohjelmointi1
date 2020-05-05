using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


/// @author Petra Puumala, Merika Peltola
/// @version 20.3.2018
/// <summary>
/// Kisun Seikkailut peli
/// </summary>
/// petolinnun kuva täältä: https://pixabay.com/en/blue-bird-flying-silver-wings-45927/
/// kisun kuva täältä: http://www.publicdomainfiles.com/show_file.php?id=13550874017348
/// koiran kuva täältä: https://pixabay.com/en/bulldog-dog-pet-angry-spiked-46363/
/// taustakuva täältä: https://pixabay.com/fi/kaupunkien-city-sininen-3164183/


public class KisunSeikkailut : PhysicsGame
{
    public bool PowerUp { get; set; }

    const double KENTANLEVEYS = 2000;
    const double KENTANKORKEUS = 600;
    const double NOPEUS = 200;
    const double POWER_US_NOPEUS = 400;
    const double HYPPY_NOPEUS = 950;
    const int RUUDUN_KOKO = 40;

    /// <summary>Pelikentän eli ruudun ("tiilen") leveys</summary>
    const double RUUDUN_LEVEYS = RUUDUN_KOKO;
    /// <summary>Pelikentän eli ruudun ("tiilen") korkeus</summary>
    const double RUUDUN_KORKEUS = RUUDUN_KOKO;

    PlatformCharacter kisu;
    IntMeter pisteLaskuri;
    EasyHighScore toplista = new EasyHighScore();

    Image taustaKuva = LoadImage("tausta");
    Image kisunKuva = LoadImage("kisu");
    Image keltaisenSaaliinKuva = LoadImage("keltainensaalis");
    Image punaisenSaaliinKuva = LoadImage("punainensaalis");
    Image koiranKuva = LoadImage("koira");
    Image herkunKuva = LoadImage("herkku");
    Image petolinnunKuva = LoadImage("petolintu");
    Image maalinKuva = LoadImage("maali");


    /// <summary>
    /// Luodaan peli
    /// </summary>
    public override void Begin()
    {
        Gravity = new Vector(0, -1000);

        LuoKentta();
        LisaaHerkku();
        LisaaNappaimet();
        LuoPistelaskuri();

        MediaPlayer.Play("taustamusa");
        Camera.Follow(kisu);
        Camera.ZoomFactor = 1;
        Camera.StayInLevel = true;
    }


    /// <summary>
    /// Luodaan pelikenttä
    /// </summary>
    public void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta1");
        kentta.SetTileMethod('X', LuoLattia);
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaSaalis, 0, punaisenSaaliinKuva, 2);
        kentta.SetTileMethod('+', LisaaSaalis, 1, keltaisenSaaliinKuva, 0);
        kentta.SetTileMethod('N', LisaaPelaaja);
        kentta.SetTileMethod('V', LisaaVihollinen, 0, petolinnunKuva, 3);
        kentta.SetTileMethod('K', LisaaVihollinen, 9, koiranKuva, 0);
        kentta.SetTileMethod('M', LisaaMaali);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        Level.Background.Image = taustaKuva;
    }


    /// <summary>
    /// Luodaan lattia/maanpinta pelitasolle
    /// </summary>
    /// <param name="paikka">Paikka, johon lattia halutaan asettaa</param>
    /// <param name="leveys">Lattia leveys</param>
    /// <param name="korkeus">Lattian korkeus</param>
    public void LuoLattia(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject lattia = PhysicsObject.CreateStaticObject(leveys, korkeus);
        lattia.Position = paikka;
        lattia.Color = Color.Gray;
        Add(lattia);
    }


    /// <summary>
    /// Luodaan tasot peliin
    /// </summary>
    /// <param name="paikka">Paikka, johon taso halutaan luoda</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus">Tason korkeus</param>
    public void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus / 30);
        taso.Position = paikka;
        taso.Color = Color.DarkBlue;
        Add(taso);
    }


    /// <summary>
    /// Lisätään saaliit peliin
    /// </summary>
    /// <param name="paikka">paikka, johon saalis lisätään</param>
    /// <param name="leveys">saaliin leveys</param>
    /// <param name="korkeus">saaliin korkeus</param>
    /// <param name="liikuSivulle">paljonko saalis liikkuu sivulle</param>
    /// <param name="kuva">saaliin kuva</param>
    /// <param name="liikuYlos">paljonko saalis liikkuu ylös</param>
    public void LisaaSaalis(Vector paikka, double leveys, double korkeus, int liikuSivulle, Image kuva, int liikuYlos)
    {
        PhysicsObject saalis = new PhysicsObject(leveys, korkeus);
        saalis.Position = paikka;
        saalis.CanRotate = false;
        saalis.Image = kuva;
        saalis.Tag = "saalis";
        Add(saalis);

        PathFollowerBrain ps = new PathFollowerBrain();
        List<Vector> psReitti = new List<Vector>();
        psReitti.Add(saalis.Position);
        Vector seuraavaPiste = new Vector(saalis.X + liikuSivulle * RUUDUN_LEVEYS, saalis.Y + liikuYlos * RUUDUN_LEVEYS);
        psReitti.Add(seuraavaPiste);
        ps.Path = psReitti;
        saalis.Brain = ps;
        ps.Loop = true;
    }


    /// <summary>
    /// Lisätään herkut peliin
    /// </summary>
    public void LisaaHerkku()
    {
        int herkut = Herkut(6);

        for (int i = 1; i <= herkut; i++)
        {
            PhysicsObject herkku = PhysicsObject.CreateStaticObject(RUUDUN_KOKO, RUUDUN_KOKO);
            herkku.Image = herkunKuva;
            herkku.Tag = "herkku";
            herkku.MakeStatic();
            herkku.Position = RandomGen.NextVector(Level.BoundingRect);
            Add(herkku);
        }

    }


    /// <summary>
    /// Luodaan listaan herkkujen lukumäärä, jonka avulla luodaan herkut
    /// </summary>
    /// <param name="maxHerkut">Herkkujen maksimimäärä</param>
    /// <returns>Herkkujen lukumäärä</returns>
    public int Herkut(int maxHerkut)
    {
        List<int> herkut = new List<int>();

        for (int i = 1; i <= maxHerkut; i++)
        {
            herkut.Add(i);
        }

        RandomGen.Shuffle(herkut);
        return herkut[3];
    }


    /// <summary>
    /// Lisätään viholliset
    /// </summary>
    /// <param name="paikka">paikka, johon vihollinen lisätään</param>
    /// <param name="leveys">vihollisen leveys</param>
    /// <param name="korkeus">vihollisen korkeus</param>
    /// <param name="liikuSivulle">paljonko vihollinen liikkuu sivulle</param>
    /// <param name="kuva">vihollisen kuva</param>
    /// <param name="liikuYlos">paljonko vihollinen liikkuu ylös</param>
    public void LisaaVihollinen(Vector paikka, double leveys, double korkeus, int liikuSivulle, Image kuva, int liikuYlos)
    {
        PhysicsObject vihollinen = new PhysicsObject(leveys, korkeus);
        vihollinen.Position = paikka;
        vihollinen.IgnoresCollisionResponse = true;
        vihollinen.CanRotate = false;
        vihollinen.Image = kuva;
        vihollinen.Tag = "vihollinen";
        Add(vihollinen);

        PathFollowerBrain pl = new PathFollowerBrain();
        List<Vector> plReitti = new List<Vector>();
        plReitti.Add(vihollinen.Position);
        Vector seuraavaPiste = new Vector(vihollinen.X + liikuSivulle * RUUDUN_LEVEYS, vihollinen.Y + liikuYlos * RUUDUN_LEVEYS);
        plReitti.Add(seuraavaPiste);
        pl.Path = plReitti;
        vihollinen.Brain = pl;
        pl.Loop = true;

    }


    /// <summary>
    /// Lisätään pelaaja/kisu
    /// </summary>
    /// <param name="paikka">Paikka, johon pelaaja/kisu lisätään</param>
    /// <param name="leveys">Kisun leveys</param>
    /// <param name="korkeus">Kisun korkeus</param>
    public void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        kisu = new PlatformCharacter(leveys, korkeus);
        kisu.Position = paikka;
        kisu.Mass = 4.0;
        kisu.Image = kisunKuva;
        AddCollisionHandler(kisu, "saalis", TormaaSaaliiseen);
        AddCollisionHandler(kisu, "vihollinen", TormaaViholliseen);
        AddCollisionHandler(kisu, "herkku", TormaaHerkkuun);
        AddCollisionHandler(kisu, "maali", TormaaMaaliin);
        Add(kisu);
    }


    /// <summary>
    /// Lisätään maali.
    /// </summary>
    /// <param name="paikka">Paikka.</param>
    /// <param name="leveys">Leveys.</param>
    /// <param name="korkeus">Korkeus.</param>
    public void LisaaMaali(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject maali = PhysicsObject.CreateStaticObject(leveys, korkeus);
        maali.Position = paikka;
        maali.IgnoresCollisionResponse = true;
        maali.Image = maalinKuva;
        maali.Tag = "maali";
        Add(maali);
    }


    /// <summary>
    /// Luodann pistelaskuri
    /// </summary>
    public void LuoPistelaskuri()
    {
        pisteLaskuri = new IntMeter(0);
        pisteLaskuri.MinValue = 0;
        pisteLaskuri.LowerLimit += KisuKuolee;

        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Right - 80;
        pisteNaytto.Y = Screen.Top - 30;
        pisteNaytto.TextColor = Color.Red;
        pisteNaytto.Color = Color.White;
        pisteNaytto.Title = "Pisteet";

        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
    }


    /// <summary>
    /// Lisätään tietyt toiminnot näppäimiin
    /// </summary>
    public void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", kisu, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu oikealle", kisu, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", kisu, HYPPY_NOPEUS);
    }


    /// <summary>
    /// Liikutetaan kisua
    /// </summary>
    /// <param name="hahmo">Kisu</param>
    /// <param name="NOPEUS">Kisun NOPEUS</param>
    /// <returns>Kisun liikkeen</returns>
    public void Liikuta(PlatformCharacter hahmo, double NOPEUS)
    {
        hahmo.Walk(NOPEUS);

        if (PowerUp == true)
        {
            hahmo.Walk(POWER_US_NOPEUS);
        }
    }


    /// <summary>
    /// Kisun hyppääminen
    /// </summary>
    /// <param name="hahmo">Kisu</param>
    /// <param name="NOPEUS">Kisun NOPEUS</param>
    /// <returns>Pelaajan/kisun hypyn</returns>
    public void Hyppaa(PlatformCharacter hahmo, double NOPEUS)
    {
        hahmo.Jump(NOPEUS);
    }


    /// <summary>
    /// Törmätään saaliiseen
    /// </summary>
    /// <param name="hahmo">Kisu</param>
    /// <param name="saalis">Saalis</param>
    public void TormaaSaaliiseen(PhysicsObject hahmo, PhysicsObject saalis)
    {
        saalis.Destroy();

        if (PowerUp == true)
        {
            pisteLaskuri.Value += 50;
        }

        pisteLaskuri.Value += 100;
    }


    /// <summary>
    /// Törmätään herkkuun
    /// </summary>
    /// <param name="hahmo">Kisu</param>
    /// <param name="herkku">Kerättävä herkku/powerup</param>
    public void TormaaHerkkuun(PhysicsObject hahmo, PhysicsObject herkku)
    {
        MessageDisplay.Add("Sait lisäboostia!");
        pisteLaskuri.Value += 10;
        PowerUp = true;
        herkku.Destroy();
        Timer powerUp = new Timer();
        powerUp.Interval = 6.0;
        powerUp.Timeout += delegate
        {
            Liikuta(kisu, NOPEUS);
            PowerUp = false;
        };
        powerUp.Start();
    }


    /// <summary>
    /// Törmätään viholliseen
    /// </summary>
    /// <param name="hahmo">Kisu</param>
    /// <param name="vihollinen">Koira tai petolintu</param>
    public void TormaaViholliseen(PhysicsObject hahmo, PhysicsObject vihollinen)
    {
        if (PowerUp == true)
        {
            pisteLaskuri.Value += 100;
        }

        pisteLaskuri.Value -= 100;
    }


    /// <summary>
    /// Törmätään maaliin
    /// </summary>
    /// <param name="hahmo">Kisu</param>
    /// <param name="maali">Maali</param>
    public void TormaaMaaliin(PhysicsObject hahmo, PhysicsObject maali)
    {
        maali.Destroy();
        hahmo.Destroy();

        toplista.EnterAndShow(pisteLaskuri.Value);
        toplista.HighScoreWindow.Closed += AloitaAlusta;
    }


    /// <summary>
    /// Kisu kuolee
    /// </summary>
    public void KisuKuolee()
    {
        AloitaAlusta2();
        PowerUp = false;
    }


    /// <summary>
    /// Pelin aloittaminen alusta maaliin pääsemisen jälkeen
    /// </summary>
    public void AloitaAlusta(Window sender)
    {
        AloitaAlusta2();
     }


    /// <summary>
    /// Pelin aloittaminen alusta
    /// </summary>
    public void AloitaAlusta2()
    {
        ClearAll();

        LuoKentta();
        LisaaHerkku();
        LisaaNappaimet();
        LuoPistelaskuri();

        Camera.Follow(kisu);
        Camera.ZoomFactor = 1;
        Camera.StayInLevel = true;
    }
}