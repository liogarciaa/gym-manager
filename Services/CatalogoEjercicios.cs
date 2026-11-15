using System.Globalization;
using System.Text;

namespace GymManager.Services;

public static class CatalogoEjercicios
{
private static readonly string[] EjerciciosBase =
[
    "Abdominales bicicleta (Bicycle Crunches)",
    "Abdominales crunch (Crunches)",
    "Abdominales en máquina (Abdominal Crunch Machine)",
    "Abducción de cadera en máquina (Hip Abduction Machine)",
    "Aducción de cadera en máquina (Hip Adduction Machine)",
    "Aperturas con mancuernas (Dumbbell Flyes)",
    "Aperturas en máquina (Machine Fly / Pec Deck)",
    "Aperturas inclinadas con mancuernas (Incline Dumbbell Flyes)",
    "Aperturas planas con mancuernas (Flat Dumbbell Flyes)",
    "Aperturas posteriores en máquina Peck Deck inverso (Reverse Pec Deck Fly)",
    "Arrancada (Snatch)",
    "Assault Bike (Air Bike)",
    "Balanceo con pesa rusa (Kettlebell Swing)",
    "Bicicleta de aire (Air Bike / Assault Bike)",
    "Bicicleta fija (Stationary Bike)",
    "Bicho muerto (Dead Bug)",
    "Buenos días con barra (Barbell Good Morning)",
    "Burpees (Burpees)",
    "Caminata en cinta (Treadmill Walk)",
    "Cargada de potencia (Power Clean)",
    "Cargada y envión (Clean and Jerk)",
    "Cinta de correr (Treadmill)",
    "Cruce de poleas altas (High-to-Low Cable Crossover)",
    "Cruce de poleas bajas (Low-to-High Cable Crossover)",
    "Cruce de poleas medias (Middle Cable Fly)",
    "Cruces inversos en polea alta (Reverse Cable Crossover)",
    "Crunch abdominal en suelo (Floor Crunch)",
    "Crunch en máquina (Abdominal Crunch Machine)",
    "Crunch en polea alta (Cable Crunch)",
    "Cuerdas de batalla (Battle Ropes)",
    "Curl alterno con mancuernas (Alternating Dumbbell Curl)",
    "Curl araña (Spider Curl)",
    "Curl Bayesiano en polea (Bayesian Cable Curl)",
    "Curl concentrado (Concentration Curl)",
    "Curl de bíceps con barra (Barbell Bicep Curl)",
    "Curl de bíceps con barra Z (EZ-Bar Curl)",
    "Curl de bíceps con mancuernas (Dumbbell Bicep Curl)",
    "Curl de bíceps en polea (Cable Bicep Curl)",
    "Curl de muñeca con barra (Barbell Wrist Curl)",
    "Curl de muñeca con mancuerna (Dumbbell Wrist Curl)",
    "Curl en banco inclinado con mancuernas (Incline Dumbbell Curl)",
    "Curl en máquina de bíceps (Machine Bicep Curl)",
    "Curl femoral acostado (Lying Leg Curl)",
    "Curl femoral de pie a una pierna (Standing Single-Leg Curl)",
    "Curl femoral sentado (Seated Leg Curl)",
    "Curl inverso con barra (Reverse Barbell Curl)",
    "Curl martillo con cuerda en polea (Rope Cable Hammer Curl)",
    "Curl martillo con mancuernas (Hammer Curl)",
    "Curl nórdico (Nordic Hamstring Curl)",
    "Curl predicador en banco Scott (Preacher Curl)",
    "Dominadas asistidas en máquina (Assisted Pull-Up Machine)",
    "Dominadas neutras (Neutral-Grip Pull-Ups)",
    "Dominadas pronas (Pull-Ups)",
    "Dominadas supinas (Chin-Ups)",
    "Elevación de cadera (Glute Bridge / Hip Thrust)",
    "Elevación de gemelos de pie (Standing Calf Raise)",
    "Elevación de gemelos en prensa (Leg Press Calf Raise)",
    "Elevación de gemelos sentado (Seated Calf Raise)",
    "Elevación de gemelos tipo burro (Donkey Calf Raise)",
    "Elevación de piernas (Leg Raises)",
    "Elevación de piernas colgado (Hanging Leg Raises)",
    "Elevación de rodillas en silla romana (Captain's Chair Knee Raises)",
    "Elevaciones frontales con barra (Barbell Front Raises)",
    "Elevaciones frontales con disco (Plate Front Raises)",
    "Elevaciones frontales con mancuernas (Dumbbell Front Raises)",
    "Elevaciones frontales en polea (Cable Front Raises)",
    "Elevaciones laterales con mancuernas (Dumbbell Lateral Raises)",
    "Elevaciones laterales en máquina (Machine Lateral Raises)",
    "Elevaciones laterales en polea baja (Cable Lateral Raises)",
    "Elevaciones posteriores con mancuernas (Rear Delt Dumbbell Flyes)",
    "Elíptica (Elliptical Trainer)",
    "Empuje de cadera con barra (Barbell Hip Thrust)",
    "Empuje de cadera en máquina (Machine Hip Thrust)",
    "Empuje de fuerza (Push Press)",
    "Empuje y arrastre de trineo (Sled Push / Sled Pull)",
    "Encogimientos de hombros con barra (Barbell Shrugs)",
    "Encogimientos de hombros con mancuernas (Dumbbell Shrugs)",
    "Encogimientos en barra hexagonal (Trap Bar Shrugs)",
    "Encogimientos en máquina Smith (Smith Machine Shrugs)",
    "Escaladora (StairMaster / Stair Climber)",
    "Escaladores (Mountain Climbers)",
    "Extensión de cadera en polea (Cable Glute Kickback)",
    "Extensión de piernas en máquina (Leg Extension)",
    "Extensión de tríceps en máquina (Seated Tricep Extension Machine)",
    "Extensión de tríceps sobre la cabeza con mancuerna (Overhead Dumbbell Tricep Extension)",
    "Extensión de tríceps sobre la cabeza en polea (Overhead Cable Tricep Extension)",
    "Extensiones de tríceps con cuerda (Cable Rope Tricep Pushdown)",
    "Extensiones de tríceps en polea (Cable Tricep Pushdown)",
    "Extensiones de tríceps en polea alta con barra (Straight-Bar Tricep Pushdown)",
    "Extensiones lumbares en banco romano (Back Extensions / Hyperextensions)",
    "Face Pull en polea alta (Cable Face Pull)",
    "Flexiones de brazos (Push-Ups)",
    "Fondos en paralelas (Parallel Bar Dips)",
    "Fondos en paralelas para pecho (Chest Dips)",
    "Fondos en paralelas para tríceps (Tricep Dips)",
    "Fondos entre bancos (Bench Dips)",
    "Fondos para tríceps (Tricep Dips)",
    "Giros rusos (Russian Twists)",
    "Hiperextensiones reversas (Reverse Hyperextensions)",
    "Hip thrust con barra (Barbell Hip Thrust)",
    "Jalón al pecho (Lat Pulldown)",
    "Jalón al pecho agarre ancho (Wide-Grip Lat Pulldown)",
    "Jalón al pecho con agarre cerrado (Close-Grip Lat Pulldown)",
    "Jalón al pecho con agarre neutro (Neutral-Grip Lat Pulldown)",
    "Jalón al pecho con agarre supino (Reverse-Grip / Supinated Lat Pulldown)",
    "Jalón unilateral en polea (Single-Arm Cable Pulldown)",
    "Jumping jacks (Jumping Jacks)",
    "Lanzamiento de balón medicinal (Wall Ball / Medicine Ball Slam)",
    "Leñadores en polea (Cable Woodchoppers)",
    "Levantamiento turco (Turkish Get-Up)",
    "Pájaros con mancuernas (Rear Delt Flyes)",
    "Paseo del granjero (Farmer's Walk)",
    "Patada de glúteo en máquina (Glute Kickback Machine)",
    "Patada de glúteo en polea baja (Cable Glute Kickback)",
    "Patada de tríceps con mancuerna (Dumbbell Tricep Kickback)",
    "Patada de tríceps en polea (Cable Tricep Kickback)",
    "Peck Deck (Pec Deck Fly)",
    "Peso muerto (Deadlift)",
    "Peso muerto a una pierna (Single-Leg RDL)",
    "Peso muerto con barra hexagonal (Trap Bar Deadlift)",
    "Peso muerto con mancuernas (Dumbbell Deadlift)",
    "Peso muerto convencional (Conventional Deadlift)",
    "Peso muerto rumano (Romanian Deadlift / RDL)",
    "Peso muerto rumano con barra (Barbell Romanian Deadlift)",
    "Peso muerto rumano con mancuernas (Dumbbell Romanian Deadlift)",
    "Peso muerto sumo (Sumo Deadlift)",
    "Plancha abdominal (Plank)",
    "Plancha lateral (Side Plank)",
    "Prensa de piernas (Leg Press)",
    "Prensa de piernas horizontal (Horizontal Leg Press)",
    "Prensa de piernas inclinada 45° (45-Degree Leg Press)",
    "Press Arnold (Arnold Press)",
    "Press cerrado (Close-Grip Bench Press)",
    "Press de banca (Bench Press)",
    "Press de banca con agarre estrecho (Close-Grip Bench Press)",
    "Press de banca con mancuernas (Dumbbell Bench Press)",
    "Press de banca declinado con barra (Decline Barbell Bench Press)",
    "Press de banca declinado con mancuernas (Decline Dumbbell Bench Press)",
    "Press de banca inclinado (Incline Bench Press)",
    "Press de banca inclinado con barra (Incline Barbell Bench Press)",
    "Press de banca plano con barra (Flat Barbell Bench Press)",
    "Press de hombros con mancuernas (Dumbbell Shoulder Press)",
    "Press de hombros en máquina (Machine Shoulder Press)",
    "Press de hombros sentado con barra (Seated Barbell Shoulder Press)",
    "Press de hombros sentado con mancuernas (Seated Dumbbell Shoulder Press)",
    "Press de pecho en máquina convergente (Converging Chest Press Machine)",
    "Press de pecho sentado en máquina (Seated Chest Press Machine)",
    "Press en máquina Smith (Smith Machine Bench Press)",
    "Press francés con barra Z (Skullcrushers / Lying Tricep Extension)",
    "Press inclinado con mancuernas (Incline Dumbbell Bench Press)",
    "Press JM (JM Press)",
    "Press militar con barra (Overhead Press / OHP)",
    "Press militar de pie con barra (Standing Barbell Overhead Press)",
    "Press Pallof en polea (Pallof Press)",
    "Press Vikingo (Landmine Shoulder Press)",
    "Puente de glúteos (Glute Bridge)",
    "Pullover con mancuerna (Dumbbell Pullover)",
    "Pullover en polea alta (Straight-Arm Cable Pulldown)",
    "Rack Pull (Rack Pull)",
    "Remo al mentón con barra (Barbell Upright Row)",
    "Remo al mentón en polea (Cable Upright Row)",
    "Remo con apoyo al pecho (Chest-Supported Row)",
    "Remo con barra (Barbell Row)",
    "Remo con barra inclinado (Bent-Over Barbell Row)",
    "Remo con barra T (T-Bar Row)",
    "Remo con mancuerna (Single-Arm Dumbbell Row)",
    "Remo en máquina (Machine Row)",
    "Remo en máquina Hammer Strength (Plate-Loaded Iso-Lateral Row)",
    "Remo ergómetro (Rowing Machine)",
    "Remo Meadows (Meadows Row)",
    "Remo Pendlay (Pendlay Row)",
    "Remo Seal (Seal Row)",
    "Remo sentado en polea (Seated Cable Row)",
    "Remo sentado en polea baja (Low Seated Cable Row)",
    "Rueda abdominal (Ab Wheel Rollout)",
    "Saltos al cajón (Box Jumps)",
    "Sentadilla (Squat)",
    "Sentadilla búlgara (Bulgarian Split Squat)",
    "Sentadilla con cinturón (Belt Squat)",
    "Sentadilla en máquina Smith (Smith Machine Squat)",
    "Sentadilla frontal con barra (Barbell Front Squat)",
    "Sentadilla goblet (Goblet Squat)",
    "Sentadilla Hack (Hack Squat)",
    "Sentadilla Jefferson (Jefferson Squat)",
    "Sentadilla Sissy (Sissy Squat)",
    "Sentadilla sumo (Sumo Squat)",
    "Sentadilla trasera con barra (Barbell Back Squat)",
    "Sentadilla Zercher (Zercher Squat)",
    "Step (Step-Ups)",
    "Subida al cajón (Box Step-Ups)",
    "Superman (Superman)",
    "Thruster con barra (Barbell Thruster)",
    "Thruster con mancuernas (Dumbbell Thruster)",
    "Tracción en polea alta (High Cable Pulldown)",
    "Zancadas caminando (Walking Lunges)",
    "Zancadas con mancuernas (Dumbbell Lunges)",
    "Zancadas inversas (Reverse Lunges)"
];

    public static IReadOnlyList<string> ObtenerSugerencias(
        string busqueda,
        IEnumerable<string>? ejerciciosGuardados = null,
        int limite = 8)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limite);
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return [];
        }

        string criterio = Normalizar(busqueda.Trim());
        var nombres = EjerciciosBase
            .Concat(ejerciciosGuardados ?? [])
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase);

        return nombres
            .Select(nombre => new { Nombre = nombre, Normalizado = Normalizar(nombre) })
            .Where(ejercicio => ejercicio.Normalizado.Contains(criterio, StringComparison.Ordinal))
            .OrderBy(ejercicio => ejercicio.Normalizado.StartsWith(criterio, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(ejercicio => ejercicio.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Take(limite)
            .Select(ejercicio => ejercicio.Nombre)
            .ToArray();
    }

    private static string Normalizar(string texto)
    {
        string descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (char caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(char.ToLowerInvariant(caracter));
            }
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
