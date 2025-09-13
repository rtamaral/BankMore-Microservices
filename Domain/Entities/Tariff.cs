using System;

namespace BankMore.Api.Domain.Entities
{
    public class Tariff
    {
        /// <summary>
        /// Identificador único da tarifa
        /// </summary>
        public Guid IdTarifa { get; set; }

        /// <summary>
        /// Identificador da conta corrente associada
        /// </summary>
        public Guid IdContaCorrente { get; set; }

        /// <summary>
        /// Data do movimento da tarifa
        /// </summary>
        public DateTime DataMovimento { get; set; }

        /// <summary>
        /// Valor da tarifa
        /// </summary>
        public decimal Valor { get; set; }
    }
}
