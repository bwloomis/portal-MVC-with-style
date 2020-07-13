using Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SubscaleCatagory = Assmnts.Reports.SharedScoring.SubscaleCatagory;

namespace Assmnts.Reports
{
    public class SisCScoring  
    {
        private static string[] sisCSubscaleNumonics = { 
            "Home", "Community", "SchoolParticipation", "SchoolLearning", "HealthAndSafety", "Social", "Advocacy"
           
            //numonics are matched to the columns in SubscaleTable below (starting with the second column)
            //two different numonics can be assigned to the same scoring scale as such: "Social/Employment"
        };

        private static int inf = 99999; //this is used as a placeholder for unobtainable scores in the table below

        //5-6 AGE COHORT
        private static double[,] SubscaleTable5To6 = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics)                                                                              percentiles
            { 16,         inf,        inf,        inf,        inf,        inf,        inf,        inf,        97.7,       },
            { 15,         3.89,       3.98,       inf,        inf,        inf,        inf,        inf,        95.2,       },
            { 14,         3.61,       3.74,       3.89,       inf,        3.95,       inf,        3.87,       90.9,       },
            { 13,         3.34,       3.5,        3.64,       3.83,       3.7,        3.75,       3.61,       84.1,       },
            { 12,         3.06,       3.26,       3.39,       3.61,       3.44,       3.47,       3.35,       74.8,       },
            { 11,         2.78,       3.02,       3.14,       3.38,       3.19,       3.18,       3.1,        63.1,       },
            { 10,         2.51,       2.78,       2.89,       3.16,       2.93,       2.9,        2.84,       50,         },
            { 9,          2.23,       2.54,       2.64,       2.93,       2.68,       2.61,       2.58,       36.9,       },
            { 8,          1.95,       2.3,        2.39,       2.71,       2.43,       2.32,       2.33,       25.2,       },
            { 7,          1.67,       2.06,       2.14,       2.48,       2.17,       2.04,       2.07,       15.9,       },
            { 6,          1.4,        1.82,       1.89,       2.25,       1.92,       1.75,       1.81,       9.1,        },
            { 5,          1.12,       1.58,       1.64,       2.03,       1.66,       1.47,       1.56,       4.8,        },
            { 4,          0.84,       1.34,       1.39,       1.8,        1.41,       1.18,       1.3,        2.3,        },
            { 3,          0.56,       1.1,        1.14,       1.58,       1.15,       0.89,       1.05,       1,          },
            { 2,          0.29,       0.86,       0.89,       1.35,       0.9,        0.61,       0.79,       0.4,        },
            { 1,          0.01,       0.62,       0.64,       1.13,       0.65,       0.32,       0.53,       0.1,        },
            { 0,          0,          0,          0,          0,          0,          0,          0,          0,          },

        };

        //7-8 AGE COHORT
        private static double[,] SubscaleTable7To8 = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics)                                                                              percentiles
            { 16,         3.98,       inf,        inf,        inf,        inf,        inf,        inf,        97.7,       },
            { 15,         3.70,       3.98,       inf,        inf,        inf,        inf,        inf,        95.2,       },
            { 14,         3.42,       3.74,       3.89,       inf,        3.95,       inf,        3.87,       90.9,       },
            { 13,         3.15,       3.50,       3.64,       3.83,       3.70,       3.75,       3.61,       84.1,       },
            { 12,         2.87,       3.26,       3.39,       3.61,       3.44,       3.47,       3.35,       74.8,       },
            { 11,         2.59,       3.02,       3.14,       3.38,       3.19,       3.18,       3.10,       63.1,       },
            { 10,         2.31,       2.78,       2.89,       3.16,       2.93,       2.90,       2.84,       50,         },
            { 9,          2.04,       2.54,       2.64,       2.93,       2.68,       2.61,       2.58,       36.9,       },
            { 8,          1.76,       2.30,       2.39,       2.71,       2.43,       2.32,       2.33,       25.2,       },
            { 7,          1.48,       2.06,       2.14,       2.48,       2.17,       2.04,       2.07,       15.9,       },
            { 6,          1.21,       1.82,       1.89,       2.25,       1.92,       1.75,       1.81,       9.1,        },
            { 5,          0.93,       1.58,       1.64,       2.03,       1.66,       1.47,       1.56,       4.8,        },
            { 4,          0.65,       1.34,       1.39,       1.80,       1.41,       1.18,       1.30,       2.3,        },
            { 3,          0.37,       1.10,       1.14,       1.58,       1.15,       0.89,       1.05,       1,          },
            { 2,          0.10,       0.86,       0.89,       1.35,       0.90,       0.61,       0.79,       0.4,        },
            { 1,          0,          0.62,       0.64,       1.13,       0.65,       0.32,       0.53,       0.1,        },
            { 0,          inf,        0,          0,          0,          0,          0,          0,          0,          },

        };

        //9-10 AGE COHORT
        private static double[,] SubscaleTable9To10 = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics)                                                                              percentiles
            { 16,         3.98,       inf,        inf,        inf,        inf,        inf,        inf,        97.7,       },
            { 15,         3.70,       3.98,       inf,        inf,        inf,        inf,        inf,        95.2,       },
            { 14,         3.42,       3.74,       3.89,       3.98,       3.95,       inf,        3.87,       90.9,       },
            { 13,         3.15,       3.50,       3.64,       3.78,       3.70,       3.75,       3.61,       84.1,       },
            { 12,         2.87,       3.26,       3.39,       3.58,       3.44,       3.47,       3.35,       74.8,       },
            { 11,         2.59,       3.02,       3.14,       3.37,       3.19,       3.18,       3.10,       63.1,       },
            { 10,         2.31,       2.78,       2.89,       3.17,       2.93,       2.90,       2.84,       50,         },
            { 9,          2.04,       2.54,       2.64,       2.96,       2.68,       2.61,       2.58,       36.9,       },
            { 8,          1.76,       2.30,       2.39,       2.76,       2.43,       2.32,       2.33,       25.2,       },
            { 7,          1.48,       2.06,       2.14,       2.56,       2.17,       2.04,       2.07,       15.9,       },
            { 6,          1.21,       1.82,       1.89,       2.35,       1.92,       1.75,       1.81,       9.1,        },
            { 5,          0.93,       1.58,       1.64,       2.15,       1.66,       1.47,       1.56,       4.8,        },
            { 4,          0.65,       1.34,       1.39,       1.94,       1.41,       1.18,       1.30,       2.3,        },
            { 3,          0.37,       1.10,       1.14,       1.74,       1.15,       0.89,       1.05,       1,          },
            { 2,          0.10,       0.86,       0.89,       1.54,       0.90,       0.61,       0.79,       0.4,        },
            { 1,          0,          0.62,       0.64,       1.33,       0.65,       0.32,       0.53,       0.1,        },
            { 0,          inf,        0,          0,          0,          0,          0,          0,          0,          },

        };

        //11-12 AGE COHORT
        private static double[,] SubscaleTable11To12 = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics)                                                                              percentiles
            { 16,         3.81,       inf,        inf,        inf,        inf,        inf,        inf,        97.7,       },
            { 15,         3.53,       3.85,       inf,        inf,        inf,        inf,        inf,        95.2,       },
            { 14,         3.25,       3.61,       3.89,       3.98,       3.81,       3.83,       3.87,       90.9,       },
            { 13,         2.97,       3.37,       3.64,       3.78,       3.56,       3.54,       3.61,       84.1,       },
            { 12,         2.70,       3.13,       3.39,       3.58,       3.30,       3.26,       3.35,       74.8,       },
            { 11,         2.42,       2.89,       3.14,       3.37,       3.05,       2.97,       3.10,       63.1,       },
            { 10,         2.14,       2.66,       2.89,       3.17,       2.79,       2.69,       2.84,       50,         },
            { 9,          1.86,       2.42,       2.64,       2.96,       2.54,       2.40,       2.58,       36.9,       },
            { 8,          1.59,       2.18,       2.39,       2.76,       2.29,       2.11,       2.33,       25.2,       },
            { 7,          1.31,       1.94,       2.14,       2.56,       2.03,       1.83,       2.07,       15.9,       },
            { 6,          1.03,       1.70,       1.89,       2.35,       1.78,       1.54,       1.81,       9.1,        },
            { 5,          0.75,       1.46,       1.64,       2.15,       1.52,       1.26,       1.56,       4.8,        },
            { 4,          0.48,       1.22,       1.39,       1.94,       1.27,       0.97,       1.30,       2.3,        },
            { 3,          0.20,       0.98,       1.14,       1.74,       1.01,       0.68,       1.05,       1,          },
            { 2,          0,          0.74,       0.89,       1.54,       0.76,       0.40,       0.79,       0.4,        },
            { 1,          inf,        0.50,       0.64,       1.33,       0.50,       0.11,       0.53,       0.1,        },
            { 0,          inf,        0,          0,          0,          0,          0,          0,          0,          },

        };

        //13-14 AGE COHORT
        private static double[,] SubscaleTable13To14 = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics)                                                                              percentiles
            { 16,         inf,        inf,        inf,        inf,        inf,        inf,        inf,        97.7,       },
            { 15,         3.71,       3.85,       inf,        inf,        inf,        inf,        inf,        95.2,       },
            { 14,         3.39,       3.61,       3.89,       3.98,       3.81,       3.83,       3.87,       90.9,       },
            { 13,         3.08,       3.37,       3.64,       3.78,       3.56,       3.54,       3.61,       84.1,       },
            { 12,         2.76,       3.13,       3.39,       3.58,       3.30,       3.26,       3.35,       74.8,       },
            { 11,         2.44,       2.89,       3.14,       3.37,       3.05,       2.97,       3.10,       63.1,       },
            { 10,         2.12,       2.66,       2.89,       3.17,       2.79,       2.69,       2.84,       50,         },
            { 9,          1.80,       2.42,       2.64,       2.96,       2.54,       2.40,       2.58,       36.9,       },
            { 8,          1.49,       2.18,       2.39,       2.76,       2.29,       2.11,       2.33,       25.2,       },
            { 7,          1.17,       1.94,       2.14,       2.56,       2.03,       1.83,       2.07,       15.9,       },
            { 6,          0.85,       1.70,       1.89,       2.35,       1.78,       1.54,       1.81,       9.1,        },
            { 5,          0.53,       1.46,       1.64,       2.15,       1.52,       1.26,       1.56,       4.8,        },
            { 4,          0.21,       1.22,       1.39,       1.94,       1.27,       0.97,       1.30,       2.3,        },
            { 3,          0,          0.98,       1.14,       1.74,       1.01,       0.68,       1.05,       1,          },
            { 2,          inf,        0.74,       0.89,       1.54,       0.76,       0.40,       0.79,       0.4,        },
            { 1,          inf,        0.50,       0.64,       1.33,       0.50,       0.11,       0.53,       0.1,        },
            { 0,          inf,        0,          0,          0,          0,          0,          0,          0,          },

        };

        //15-16 AGE COHORT
        private static double[,] SubscaleTable15To16 = {
            
            //standard scores       minimum subscale raw scores (same order as subscaleNumonics)                                                                              percentiles
            { 16,         3.78,       inf,        inf,        inf,        inf,        inf,        inf,        97.7,       },
            { 15,         3.46,       3.8,        inf,        inf,        inf,        inf,        inf,        95.2,       },
            { 14,         3.14,       3.53,       3.78,       inf,        3.76,       3.75,       3.77,       90.9,       },
            { 13,         2.83,       3.26,       3.48,       3.76,       3.46,       3.42,       3.48,       84.1,       },
            { 12,         2.51,       3.00,       3.19,       3.51,       3.15,       3.09,       3.19,       74.8,       },
            { 11,         2.19,       2.73,       2.89,       3.26,       2.85,       2.75,       2.90,       63.1,       },
            { 10,         1.87,       2.46,       2.59,       3.01,       2.55,       2.42,       2.61,       50,         },
            { 9,          1.55,       2.19,       2.30,       2.76,       2.24,       2.09,       2.32,       36.9,       },
            { 8,          1.24,       1.93,       2.00,       2.51,       1.94,       1.76,       2.03,       25.2,       },
            { 7,          0.92,       1.66,       1.70,       2.26,       1.63,       1.43,       1.74,       15.9,       },
            { 6,          0.60,       1.39,       1.40,       2.01,       1.33,       1.10,       1.45,       9.1,        },
            { 5,          0.28,       1.13,       1.11,       1.76,       1.03,       0.77,       1.16,       4.8,        },
            { 4,          0,          0.86,       0.81,       1.52,       0.72,       0.44,       0.87,       2.3,        },
            { 3,          inf,        0.59,       0.51,       1.27,       0.42,       0.10,       0.59,       1,          },
            { 2,          inf,        0.33,       0.21,       1.02,       0.11,       0,          0.30,       0.4,        },
            { 1,          inf,        0.06,       0,          0.77,       0,          inf,        0.01,       0.1,        },
            { 0,          inf,        0,          inf,        0,          inf,        inf,        0,          0,          },

        };

        private static double[,] CompositeTable = {
       //standardScore  //ages 5-6          //ages 7-8          //ages 9-10         //ages 11-12        //ages 13-14        //ages 15-16  
                        //PR      //Score   //PR      //Score   //PR      //Score   //PR      //Score   //PR      //Score   //PR      //Score                 
            { 125,      inf,      inf,      inf,      inf,      inf,      inf,      95.2,     3.98,     95.2,     3.99,     95.2,     3.98,     },
            { 124,      inf,      inf,      inf,      inf,      94.5,     3.95,     94.5,     3.94,     94.5,     3.95,     94.5,     3.93,     },
            { 123,      inf,      inf,      93.7,     3.99,     93.7,     3.91,     93.7,     3.89,     93.7,     3.90,     93.7,     3.87,     },
            { 122,      inf,      inf,      92.9,     3.94,     92.9,     3.87,     92.9,     3.85,     92.9,     3.85,     92.9,     3.82,     },
            { 121,      inf,      inf,      91.9,     3.90,     91.9,     3.82,     91.9,     3.80,     91.9,     3.80,     91.9,     3.76,     },
            { 120,      90.9,     3.96,     90.9,     3.85,     90.9,     3.78,     90.9,     3.76,     90.9,     3.76,     90.9,     3.71,     },
            { 119,      89.7,     3.91,     89.7,     3.81,     89.7,     3.74,     89.7,     3.71,     89.7,     3.71,     89.7,     3.66,     },
            { 118,      88.5,     3.86,     88.5,     3.76,     88.5,     3.70,     88.5,     3.66,     88.5,     3.66,     88.5,     3.60,     },
            { 117,      87.1,     3.81,     87.1,     3.72,     87.1,     3.65,     87.1,     3.62,     87.1,     3.61,     87.1,     3.55,     },
            { 116,      85.7,     3.77,     85.7,     3.67,     85.7,     3.61,     85.7,     3.57,     85.7,     3.57,     85.7,     3.49,     },
            { 115,      84.1,     3.72,     84.1,     3.63,     84.1,     3.57,     84.1,     3.53,     84.1,     3.52,     84.1,     3.44,     },
            { 114,      82.5,     3.67,     82.5,     3.58,     82.5,     3.52,     82.5,     3.48,     82.5,     3.47,     82.5,     3.38,     },
            { 113,      80.7,     3.62,     80.7,     3.54,     80.7,     3.48,     80.7,     3.44,     80.7,     3.42,     80.7,     3.33,     },
            { 112,      78.8,     3.58,     78.8,     3.49,     78.8,     3.44,     78.8,     3.39,     78.8,     3.38,     78.8,     3.28,     },
            { 111,      76.8,     3.53,     76.8,     3.45,     76.8,     3.40,     76.8,     3.35,     76.8,     3.33,     76.8,     3.22,     },
            { 110,      74.8,     3.48,     74.8,     3.40,     74.8,     3.35,     74.8,     3.30,     74.8,     3.28,     74.8,     3.17,     },
            { 109,      72.6,     3.43,     72.6,     3.36,     72.6,     3.31,     72.6,     3.26,     72.6,     3.23,     72.6,     3.11,     },
            { 108,      70.3,     3.39,     70.3,     3.31,     70.3,     3.27,     70.3,     3.21,     70.3,     3.19,     70.3,     3.06,     },
            { 107,      68,       3.34,     68,       3.27,     68,       3.22,     68,       3.17,     68,       3.14,     68,       3.00,     },
            { 106,      65.5,     3.29,     65.5,     3.22,     65.5,     3.18,     65.5,     3.12,     65.5,     3.09,     65.5,     2.95,     },
            { 105,      63.1,     3.24,     63.1,     3.18,     63.1,     3.14,     63.1,     3.08,     63.1,     3.04,     63.1,     2.90,     },
            { 104,      60.5,     3.19,     60.5,     3.13,     60.5,     3.10,     60.5,     3.03,     60.5,     3.00,     60.5,     2.84,     },
            { 103,      57.9,     3.15,     57.9,     3.09,     57.9,     3.05,     57.9,     2.99,     57.9,     2.95,     57.9,     2.79,     },
            { 102,      55.3,     3.10,     55.3,     3.04,     55.3,     3.01,     55.3,     2.94,     55.3,     2.90,     55.3,     2.73,     },
            { 101,      52.7,     3.05,     52.7,     3.00,     52.7,     2.97,     52.7,     2.90,     52.7,     2.85,     52.7,     2.68,     },
            { 100,      50,       3.00,     50,       2.95,     50,       2.92,     50,       2.85,     50,       2.81,     50,       2.62,     },
            { 99,       47.3,     2.96,     47.3,     2.91,     47.3,     2.88,     47.3,     2.81,     47.3,     2.76,     47.3,     2.57,     },
            { 98,       44.7,     2.91,     44.7,     2.86,     44.7,     2.84,     44.7,     2.76,     44.7,     2.71,     44.7,     2.52,     },
            { 97,       42.1,     2.86,     42.1,     2.82,     42.1,     2.80,     42.1,     2.72,     42.1,     2.66,     42.1,     2.46,     },
            { 96,       39.5,     2.81,     39.5,     2.77,     39.5,     2.75,     39.5,     2.67,     39.5,     2.62,     39.5,     2.41,     },
            { 95,       36.9,     2.76,     36.9,     2.73,     36.9,     2.71,     36.9,     2.63,     36.9,     2.57,     36.9,     2.35,     },
            { 94,       34.5,     2.72,     34.5,     2.68,     34.5,     2.67,     34.5,     2.58,     34.5,     2.52,     34.5,     2.30,     },
            { 93,       32,       2.67,     32,       2.64,     32,       2.62,     32,       2.54,     32,       2.47,     32,       2.25,     },
            { 92,       29.7,     2.62,     29.7,     2.59,     29.7,     2.58,     29.7,     2.49,     29.7,     2.42,     29.7,     2.19,     },
            { 91,       27.4,     2.57,     27.4,     2.55,     27.4,     2.54,     27.4,     2.45,     27.4,     2.38,     27.4,     2.14,     },
            { 90,       25.2,     2.53,     25.2,     2.50,     25.2,     2.50,     25.2,     2.40,     25.2,     2.33,     25.2,     2.08,     },
            { 89,       23.2,     2.48,     23.2,     2.46,     23.2,     2.45,     23.2,     2.36,     23.2,     2.28,     23.2,     2.03,     },
            { 88,       21.2,     2.43,     21.2,     2.41,     21.2,     2.41,     21.2,     2.31,     21.2,     2.23,     21.2,     1.97,     },
            { 87,       19.3,     2.38,     19.3,     2.37,     19.3,     2.37,     19.3,     2.27,     19.3,     2.19,     19.3,     1.92,     },
            { 86,       17.5,     2.33,     17.5,     2.32,     17.5,     2.32,     17.5,     2.22,     17.5,     2.14,     17.5,     1.87,     },
            { 85,       15.9,     2.29,     15.9,     2.28,     15.9,     2.28,     15.9,     2.18,     15.9,     2.09,     15.9,     1.81,     },
            { 84,       14.3,     2.24,     14.3,     2.23,     14.3,     2.24,     14.3,     2.13,     14.3,     2.04,     14.3,     1.76,     },
            { 83,       12.9,     2.19,     12.9,     2.19,     12.9,     2.20,     12.9,     2.09,     12.9,     2.00,     12.9,     1.70,     },
            { 82,       11.5,     2.14,     11.5,     2.14,     11.5,     2.15,     11.5,     2.04,     11.5,     1.95,     11.5,     1.65,     },
            { 81,       10.3,     2.10,     10.3,     2.10,     10.3,     2.11,     10.3,     1.99,     10.3,     1.90,     10.3,     1.59,     },
            { 80,       9.1,      2.05,     9.1,      2.05,     9.1,      2.07,     9.1,      1.95,     9.1,      1.85,     9.1,      1.54,     },
            { 79,       8.1,      2.00,     8.1,      2.01,     8.1,      2.02,     8.1,      1.90,     8.1,      1.81,     8.1,      1.49,     },
            { 78,       7.1,      1.95,     7.1,      1.96,     7.1,      1.98,     7.1,      1.86,     7.1,      1.76,     7.1,      1.43,     },
            { 77,       6.3,      1.91,     6.3,      1.92,     6.3,      1.94,     6.3,      1.81,     6.3,      1.71,     6.3,      1.38,     },
            { 76,       5.5,      1.86,     5.5,      1.87,     5.5,      1.90,     5.5,      1.77,     5.5,      1.66,     5.5,      1.32,     },
            { 75,       4.8,      1.81,     4.8,      1.83,     4.8,      1.85,     4.8,      1.72,     4.8,      1.62,     4.8,      1.27,     },
            { 74,       4.2,      1.76,     4.2,      1.78,     4.2,      1.81,     4.2,      1.68,     4.2,      1.57,     4.2,      1.21,     },
            { 73,       3.6,      1.71,     3.6,      1.74,     3.6,      1.77,     3.6,      1.63,     3.6,      1.52,     3.6,      1.16,     },
            { 72,       3.1,      1.67,     3.1,      1.69,     3.1,      1.72,     3.1,      1.59,     3.1,      1.47,     3.1,      1.11,     },
            { 71,       2.7,      1.62,     2.7,      1.65,     2.7,      1.68,     2.7,      1.54,     2.7,      1.43,     2.7,      1.05,     },
            { 70,       2.3,      1.57,     2.3,      1.60,     2.3,      1.64,     2.3,      1.50,     2.3,      1.38,     2.3,      1.00,     },
            { 69,       1.9,      1.52,     1.9,      1.56,     1.9,      1.60,     1.9,      1.45,     1.9,      1.33,     1.9,      0.94,     },
            { 68,       1.6,      1.48,     1.6,      1.51,     1.6,      1.55,     1.6,      1.41,     1.6,      1.28,     1.6,      0.89,     },
            { 67,       1.4,      1.43,     1.4,      1.47,     1.4,      1.51,     1.4,      1.36,     1.4,      1.24,     1.4,      0.83,     },
            { 66,       1.2,      1.38,     1.2,      1.42,     1.2,      1.47,     1.2,      1.32,     1.2,      1.19,     1.2,      0.78,     },
            { 65,       1,        1.33,     1,        1.38,     1,        1.42,     1,        1.27,     1,        1.14,     1,        0.73,     },
            { 64,       0.8,      1.28,     0.8,      1.33,     0.8,      1.38,     0.8,      1.23,     0.8,      1.09,     0.8,      0.67,     },
            { 63,       0.7,      1.24,     0.7,      1.29,     0.7,      1.34,     0.7,      1.18,     0.7,      1.05,     0.7,      0.62,     },
            { 62,       0.6,      1.19,     0.6,      1.24,     0.6,      1.30,     0.6,      1.14,     0.6,      1.00,     0.6,      0.56,     },
            { 61,       0.5,      1.14,     0.5,      1.20,     0.5,      1.25,     0.5,      1.09,     0.5,      0.95,     0.5,      0.51,     },
            { 60,       0.4,      1.09,     0.4,      1.15,     0.4,      1.21,     0.4,      1.05,     0.4,      0.90,     0.4,      0.45,     },
            { 59,       0.3,      1.05,     0.3,      1.11,     0.3,      1.17,     0.3,      1.00,     0.3,      0.86,     0.3,      0.40,     },
            { 58,       0.3,      1.00,     0.3,      1.06,     0.3,      1.12,     0.3,      0.96,     0.3,      0.81,     0.3,      0.35,     },
            { 57,       0.2,      0.95,     0.2,      1.02,     0.2,      1.08,     0.2,      0.91,     0.2,      0.76,     0.2,      0.29,     },
            { 56,       0.2,      0.90,     0.2,      0.97,     0.2,      1.04,     0.2,      0.87,     0.2,      0.71,     0.2,      0.24,     },
            { 55,       0.1,      0.86,     0.1,      0.93,     0.1,      1.00,     0.1,      0.82,     0.1,      0.67,     0.1,      0.18,     },
            { 54,       0.1,      0.81,     0.1,      0.88,     0.1,      0.95,     0.1,      0.78,     0.1,      0.62,     0.1,      0.13,     },
            { 53,       0.1,      0.76,     0.1,      0.84,     0.1,      0.91,     0.1,      0.73,     0.1,      0.57,     0.1,      0.07,     },
            { 52,       0.1,      0.71,     0.1,      0.79,     0.1,      0.87,     0.1,      0.69,     0.1,      0.52,     0.1,      0.02,     },
            { 51,       0.1,      0.66,     0.1,      0.75,     0.1,      0.82,     0.1,      0.64,     0.1,      0.48,     0.1,      0,        },
            { 50,       0,        0.62,     0,        0.70,     0,        0.78,     0,        0.60,     0,        0.43,     inf,      inf,      },
            { 49,       0,        0.57,     0,        0.66,     0,        0.74,     0,        0.55,     0,        0.38,     inf,      inf,      },
            { 48,       0,        0.52,     0,        0.61,     0,        0.70,     0,        0.51,     0,        0.33,     inf,      inf,      },
            { 47,       0,        0.47,     0,        0.57,     0,        0.65,     0,        0.46,     0,        0.29,     inf,      inf,      },
            { 46,       0,        0.43,     0,        0.52,     0,        0.61,     0,        0.42,     0,        0.24,     inf,      inf,      },
            { 45,       0,        0.38,     0,        0.48,     0,        0.57,     0,        0.37,     0,        0.19,     inf,      inf,      },
            { 44,       0,        0.33,     0,        0.43,     0,        0.52,     0,        0.32,     0,        0.14,     inf,      inf,      },
            { 43,       0,        0.28,     0,        0.39,     0,        0.48,     0,        0.28,     0,        0.10,     inf,      inf,      },
            { 42,       0,        0.23,     0,        0.34,     0,        0.44,     0,        0.23,     0,        0.05,     inf,      inf,      },
            { 41,       0,        0.19,     0,        0.30,     0,        0.40,     0,        0.19,     0,        0,        inf,      inf,      },
            { 40,       0,        0.14,     0,        0.25,     0,        0.35,     0,        0.14,     inf,      inf,      inf,      inf,      },
            { 39,       0,        0.09,     0,        0.21,     0,        0.31,     0,        0.10,     inf,      inf,      inf,      inf,      },
            { 38,       0,        0.04,     0,        0.16,     0,        0.27,     0,        0.05,     inf,      inf,      inf,      inf,      },
            { 37,       0,        0,        0,        0.12,     0,        0.22,     0,        0.01,     inf,      inf,      inf,      inf,      },
            { 36,       inf,      inf,      0,        0.07,     0,        0.18,     0,        0,        inf,      inf,      inf,      inf,      },
            { 35,       inf,      inf,      0,        0.03,     0,        0.14,     inf,      inf,      inf,      inf,      inf,      inf,      },
            { 34,       inf,      inf,      0,        0,        0,        0.10,     inf,      inf,      inf,      inf,      inf,      inf,      },
            { 33,       inf,      inf,      inf,      inf,      0,        0.05,     inf,      inf,      inf,      inf,      inf,      inf,      },
            { 32,       inf,      inf,      inf,      inf,      0,        0.01,     inf,      inf,      inf,      inf,      inf,      inf,      },
            { 31,       inf,      inf,      inf,      inf,      0,        0,        inf,      inf,      inf,      inf,      inf,      inf,      },

        };

        private static SubscaleCatagory[] sisCCatagories = null;
        public static SubscaleCatagory[] getSisCSubscaleCatagories()
        {
            if (sisCCatagories == null)
                sisCCatagories = SubscaleCatagory.getCatagoriesFromNumonics(sisCSubscaleNumonics);
            return (SubscaleCatagory[])sisCCatagories.Clone();
        }

        public static void UpdateSisCScores(IFormsRepository formsRepo, int formResultId, int ageInYears)
        {
            def_Forms frm = formsRepo.GetFormByIdentifier("SIS-C");
            SharedScoring.UpdateSisScoresNoSave
            (
                formsRepo, frm, formResultId,
                getSisCSubscaleCatagories(),
                (int totalRawScore, double avgRawScore, SubscaleCatagory cat) => { return GetSisCSubscaleStandardScore(avgRawScore, cat, ageInYears); },
                (int totalRawScore, double avgRawScore, SubscaleCatagory cat) => { return GetSisCSubscalePercentile(avgRawScore, cat, ageInYears); }
            );

            def_ResponseVariables rv = formsRepo.GetResponseVariablesByFormResultIdentifier(formResultId, "scr_total_rating");
            double totalRating = Convert.ToDouble(rv.rspValue);
            double meanRating = Math.Round(totalRating / 7, 2); //usd only for SIS-C reports

            //save standard scores to database
            double compositeIndex = GetSisCSupportNeedsIndex(meanRating, ageInYears);
            double compositePercentile = GetSisCSupportNeedsPercentile(meanRating, ageInYears);
            SharedScoring.UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_support_needs_index", compositeIndex);
            SharedScoring.UpdateScoreResponseNoSave(formsRepo, formResultId, "scr_sni_percentile_rank", compositePercentile);

            formsRepo.Save();
        }

        private static double[,] getSubscaleTableForAge( int ageInYears)
        {
            if (ageInYears ==  5 || ageInYears ==  6 )
                return SubscaleTable5To6;
            if (ageInYears ==  7 || ageInYears ==  8 )
                return SubscaleTable7To8;
            if (ageInYears ==  9 || ageInYears == 10 )
                return SubscaleTable9To10;
            if (ageInYears == 11 || ageInYears == 12 )
                return SubscaleTable11To12;
            if (ageInYears == 13 || ageInYears == 14 )
                return SubscaleTable13To14;
            if (ageInYears == 15 || ageInYears == 16 )
                return SubscaleTable15To16;
            throw new Exception("unsupported age (" + ageInYears + " years)");
        }

        public static int getCompositeTablePRColumn(int ageInYears)
        {
            if (ageInYears < 5 || ageInYears > 16)
                throw new Exception("unsupported age (" + ageInYears + " years)");
            return (ageInYears - 5) / 2 * 2 + 1;
        }

        public static int getCompositeTableScoreColumn(int ageInYears)
        {
            if (ageInYears < 5 || ageInYears > 16)
                throw new Exception("unsupported age (" + ageInYears + " years)");
            return (ageInYears - 5) / 2 * 2 + 2;
        }
        
        public static double GetSisCSubscaleStandardScore( double avgRawScore, SubscaleCatagory cat, int ageInYears)
        {
            double[,] table = getSubscaleTableForAge(ageInYears);
            int SubScaleTableRowCount = table.Length / 9;
            int i = (int)cat.index + 1;
            for (int rowIndex = 0; rowIndex < SubScaleTableRowCount; rowIndex++)
            {
                if (table[rowIndex, i] <= avgRawScore)
                    return table[rowIndex, 0];
            }
            return 0;
        }

        public static double GetSisCSubscalePercentile(double avgRawScore, SubscaleCatagory cat, int ageInYears)
        {
            double[,] table = getSubscaleTableForAge(ageInYears);
            int SubScaleTableRowCount = table.Length / 9;
            int i = (int)cat.index + 1;
            for (int rowIndex = 0; rowIndex < SubScaleTableRowCount; rowIndex++)
            {
                if (table[rowIndex, i] <= avgRawScore)
                    return table[rowIndex, sisCSubscaleNumonics.Length + 1];
            }
            return 0;
        }

        public static double GetSisCSupportNeedsIndex(double avgScore, int ageInYears)
        {
            int CompositeTableRowCount = CompositeTable.Length / 13;
            int scoreCol = getCompositeTableScoreColumn(ageInYears);
            int resultCol = 0;
            for (int rowIndex = 0; rowIndex < CompositeTableRowCount; rowIndex++)
            {
                if (CompositeTable[rowIndex, scoreCol] <= avgScore)
                    return CompositeTable[rowIndex, resultCol];
            }
            return 0;
        }

        public static double GetSisCSupportNeedsPercentile(double avgScore, int ageInYears)
        {
            int CompositeTableRowCount = CompositeTable.Length / 13;
            int scoreCol = getCompositeTableScoreColumn(ageInYears);
            int resultCol = getCompositeTablePRColumn(ageInYears);
            for (int rowIndex = 0; rowIndex < CompositeTableRowCount; rowIndex++)
            {
                if (CompositeTable[rowIndex, scoreCol] <= avgScore)
                    return CompositeTable[rowIndex, resultCol];
            }
            return 0;
        }
    }
}