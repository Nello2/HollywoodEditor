using PropertyChanged;
using System;

namespace HollywoodEditor.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Professions
    {
        public enum Profession
        {
            Actor,
            Composer,
            Scriptwriter,
            Cinematographer,
            FilmEditor,
            Producer,
            Director,
            Agent,
            LieutScript,
            LieutPrep,
            LieutProd,
            LieutPost,
            LieutRelease,
            LieutSecurity,
            LieutProducers,
            LieutInfrastructure,
            LieutTech,
            LieutMuseum,
            LieutEscort,
            CptHR,
            CptLawyer,
            CptFinancier,
            CptPR,
            Else
        }
        public string Name { get; set; }
        public double Value { get; set; }
        public string ProfToDecode
        {
            get
            {
                string pr = "PROFESSION_";
                if (GetProfession != Profession.Else)
                    return pr + Name.ToUpper();
                else
                    return pr + "NONE";
            }
        }
        public Profession GetProfession
        {
            get
            {
                switch (Name)
                {
                    case "Actor":
                        return Profession.Actor;
                    case "Composer":
                        return Profession.Composer;
                    case "Scriptwriter":
                        return Profession.Scriptwriter;
                    case "Cinematographer":
                        return Profession.Cinematographer;
                    case "FilmEditor":
                        return Profession.FilmEditor;
                    case "Producer":
                        return Profession.Producer;
                    case "Director":
                        return Profession.Director;
                    case "Agent":
                        return Profession.Agent;
                    case "LieutScript":
                        return Profession.LieutScript;
                    case "LieutPrep":
                        return Profession.LieutPrep;
                    case "LieutProd":
                        return Profession.LieutProd;
                    case "LieutPost":
                        return Profession.LieutPost;
                    case "LieutRelease":
                        return Profession.LieutRelease;
                    case "LieutSecurity":
                        return Profession.LieutSecurity;
                    case "LieutProducers":
                        return Profession.LieutProducers;
                    case "LieutInfrastructure":
                        return Profession.LieutInfrastructure;
                    case "LieutTech":
                        return Profession.LieutTech;
                    case "LieutMuseum":
                        return Profession.LieutMuseum;
                    case "LieutEscort":
                        return Profession.LieutEscort;
                    case "CptHR":
                        return Profession.CptHR;
                    case "CptLawyer":
                        return Profession.CptLawyer;
                    case "CptFinancier":
                        return Profession.CptFinancier;
                    case "CptPR":
                        return Profession.CptPR;
                    default:
                        return Profession.Else;
                }
            }

        }
        public bool IsTalent
        {
            get
            {
                switch (GetProfession)
                {
                    case Profession.Actor:
                    case Profession.Composer:
                    case Profession.Scriptwriter:
                    case Profession.Cinematographer:
                    case Profession.FilmEditor:
                    case Profession.Producer:
                    case Profession.Director:
                    case Profession.Agent:
                        return true;
                    case Profession.LieutScript:
                    case Profession.LieutPrep:
                    case Profession.LieutProd:
                    case Profession.LieutPost:
                    case Profession.LieutRelease:
                    case Profession.LieutSecurity:
                    case Profession.LieutProducers:
                    case Profession.LieutInfrastructure:
                    case Profession.LieutTech:
                    case Profession.LieutMuseum:
                    case Profession.LieutEscort:
                    case Profession.CptHR:
                    case Profession.CptLawyer:
                    case Profession.CptFinancier:
                    case Profession.CptPR:
                    case Profession.Else:
                    default:
                        return false;
                }
            }
        }

        public static bool operator ==(Professions a, Professions b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

            return b.Name == a.Name &&
                   b.Value == a.Value;
        }

        public static bool operator !=(Professions a, Professions b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return this == (Professions)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Name != null ? Name.GetHashCode() : 0);
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
        public void SetValueWithoutCheck(double newValue)
        {
            if (Math.Abs(Value - newValue) < 0.001) return;
            Value = newValue;
        }
    }
}