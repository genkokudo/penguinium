using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Penguinium.ApiBridge;
using Penguinium.Common;

namespace Penguinium.ProcessData
{

    /// <summary>
    /// 自分の注文情報
    /// 親情報、子情報の両方に対応している
    /// </summary>
    public class Order
    {
        public Order(string _orderId)
        {
            OrderId = _orderId;
        }
        public Order(RawChildOrder _order)
        {
            OrderId = _order.child_order_acceptance_id; // 注文受付ID
            ChildOrderId = _order.child_order_id; // 約定ID
            Date = DateTime.Parse(_order.child_order_date + "+00:00"); // 注文日時
            Side = (OrderSide)Enum.Parse(typeof(OrderSide), _order.side, true);
            Price = (int)_order.price;
            Size = _order.size;
            ExecutedSize = _order.executed_size;
            OutstandingSize = _order.outstanding_size;
        }
        public Order(RawParentOrder _order)
        {
            OrderId = _order.parent_order_acceptance_id; // 注文受付ID
            ChildOrderId = _order.parent_order_id; // 約定ID
            Date = DateTime.Parse(_order.parent_order_date + "+00:00"); // 注文日時
            Side = (OrderSide)Enum.Parse(typeof(OrderSide), _order.side, true);
            Price = (int)_order.price;
            Size = _order.size;
            ExecutedSize = _order.executed_size;
            OutstandingSize = _order.outstanding_size;
        }

        #region プロパティ
        public string OrderId { get; set; } // 注文受付ID
        public string ChildOrderId { get; set; } // 約定ID
        public DateTime Date { get; set; } // 注文日時
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderSide Side { get; set; } // 売・買
        public int Price { get; set; } // 価格
        public double Size { get; set; } // 数量
        public double ExecutedSize { get; set; } // 約定済数量
        public double OutstandingSize { get; set; } // 残数量
        #endregion

        public override string ToString()
        {
            return "Order" + JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// 注文受付結果
    /// </summary>
    public class OrderResult
    {
        public OrderResult(RawOrderResult _result)
        {
            if (String.IsNullOrEmpty(_result.child_order_acceptance_id))
            {
                this.OrderId = _result.parent_order_acceptance_id;
            }
            else
            {
                this.OrderId = _result.child_order_acceptance_id;
            }
        }

        public string OrderId { get; set; } // 注文受付ID

        public override string ToString()
        {
            return "OrderResult" + JsonConvert.SerializeObject(this);
        }
    }
}
